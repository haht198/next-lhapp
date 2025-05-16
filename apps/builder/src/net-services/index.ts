// Config

import { execSync, spawn } from 'child_process';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'fs';
import { arch, platform, version } from 'os';
import path from 'path';
import { v4 } from 'uuid';
import { AwsS3Client } from '../aws-s3';
import { S3_RELEASE_APP_BUCKET } from '../constants';
import { BuildInfo } from '../models/build-info';
import { executeCommand } from '../utils/shell';
import { dotnetCommans } from './command';
import { SigningService } from './signing';
import { zipFolder } from '../utils/fs';

export interface NetServiceConfig {
  name: string;
  source: string;
  executeFile: string;
}

export interface NetServicesBuildConfig {
  services: NetServiceConfig[];
  deployDir: string;
}

export class NetServicesBuilder {
  private config: NetServicesBuildConfig;

  private readonly buildInfoFilePath = path.join(
    __dirname,
    '../../../../build-info.json'
  );
  private readonly signServiceExtension =
    platform() === 'darwin' ? '.dylib' : '.dll';

  private tempBuildInfo: BuildInfo | null = null;
  private projectVersion: string | null = null;

  constructor(config: NetServicesBuildConfig) {
    this.config = config;
  }

  public async start() {
    console.log('[NetServicesBuilder] Start build net services with config:', {
      ...this.config,
      signServiceExtension: this.signServiceExtension,
      platform: platform(),
      arch: arch(),
    });

    if (!AwsS3Client.s3) {
      await AwsS3Client.init();
    }

    this.validateBuild(this.config);
    console.log(
      '[NetServicesBuilder] - Start: Validate build configuration ✅'
    );

    // await this.testListVersions();
    await this.prepareBuildInfo();

    // process.exit(0);
    // return;

    // log command
    await executeCommand(`dotnet --info`);
    const needSignFiles = [];
    let signResult = true;
    for (const service of this.config.services) {
      const result = await this.build(service, this.config.deployDir);
      if (!result) {
        throw new Error(
          `[NetServicesBuilder] - Start: Build failed with code ${result}`
        );
      }
      const executable = path.join(this.config.deployDir, `${service.executeFile}`);
      if (existsSync(executable)) {
        needSignFiles.push(executable);
      }
      console.log(`[NetServicesBuilder] - Build: Need sign files: ${needSignFiles}`);
      if (needSignFiles.length > 0) {
        signResult = await SigningService.signFiles(needSignFiles);
      }
      if (!signResult) {
        throw new Error(
          `[NetServicesBuilder] - Publish failed: Code sign failed`
        );
      }
      console.log(`[NetServicesBuilder] - Publish: Code sign result: ${signResult} ✅`);
      const zipFilePath = path.join(this.config.deployDir, `${this.getPublishServiceName()}.zip`);
      console.log(`[NetServicesBuilder] - Compress publish files into zip file: ${zipFilePath}`);
      const zipResult = await zipFolder(this.config.deployDir, zipFilePath);
      if (!zipResult) {
        throw new Error(
          `[NetServicesBuilder] - Publish failed: Zip failed`
        );
      }
      console.log(`[NetServicesBuilder] - Publish: Zip file: ${zipFilePath} ✅`);
    }
  }

  public async build(service: NetServiceConfig, output: string) {
    console.log(
      `[NetServicesBuilder] - Build: Start build net service [${service.name}] ...`
    );
    // Check exists source file
    if (!existsSync(service.source)) {
      const errorMessage = `Source file: ${service.source} not found. Please check the source file path.`;
      console.error(errorMessage);
      throw new Error(errorMessage);
    }
    console.log(
      `[NetServicesBuilder] - Build: Source file: ${service.source} ✅`
    );

    // Check exists output directory
    if (!existsSync(output)) {
      // Create output directory
      mkdirSync(output, { recursive: true });
    }
    console.log(`[NetServicesBuilder] - Build: Output directory: ${output} ✅`);
    try {
      const restoreCode = await this.dotnetInvokeCommand(
        dotnetCommans.RESTORE,
        service.source
      );
      if (restoreCode !== 0) {
        throw new Error(
          `[NetServicesBuilder] - Build: Restore failed with code ${restoreCode}`
        );
      }
      console.log(
        `[NetServicesBuilder] - Build: Restore code: ${restoreCode} ✅`
      );

      const buildCode = await this.dotnetInvokeCommand(
        dotnetCommans.BUILD,
        service.source
      );
      if (buildCode !== 0) {
        throw new Error(
          `[NetServicesBuilder] - Build: Build failed with code ${buildCode}`
        );
      }
      console.log(`[NetServicesBuilder] - Build: Build code: ${buildCode} ✅`);

      const publishCode = await this.dotnetInvokeCommand(
        dotnetCommans.PUBLISH,
        service.source,
        '-c Release',
        `-r ${platform() === 'win32' ? 'win-x64' : 'osx-arm64'}`, // todo check architecture
        '--self-contained=true',
        `-p:Version=${this.tempBuildInfo?.version}`,
        `-o ${output}`
      );

      if (publishCode !== 0) {
        throw new Error(
          `[NetServicesBuilder] - Build: Publish failed with code ${publishCode}`
        );
      }
      console.log(
        `[NetServicesBuilder] - Build: Publish code: ${publishCode} ✅`
      );

      return true;
      ///
    } catch (error) {
      console.error(`[NetServicesBuilder] - Build: Error: ${error}`);
    }
  }

  private validateBuild(config: NetServicesBuildConfig) {
    if (!config.services.length) {
      throw new Error('No services to build. Please check the services array.');
    }

    if (!config.deployDir) {
      throw new Error(
        'Deploy path is required. Please check the deployDir property.'
      );
    }
  }

  private async prepareBuildInfo() {
    const {content, error} = await AwsS3Client.readFile(S3_RELEASE_APP_BUCKET, 'released-services/dev/mac/last-build-info.json');
    let buildInfo: BuildInfo | null = content ? JSON.parse(content) : null;
    if (error && !content) {
        console.log('[NetServicesBuilder] - Last build info error:', error);
        if (error.name === 'NoSuchKey') {
            console.log('[NetServicesBuilder] - Last build info not found. Uploading new build info to s3...');
            buildInfo = await this.initBuildInfo();
            const uploadResult = await AwsS3Client.uploadFile(S3_RELEASE_APP_BUCKET, 'released-services/dev/mac/last-build-info.json', this.buildInfoFilePath);
            console.log('[NetServicesBuilder] - Upload build info to s3:', uploadResult);
        } else {
            console.error('[NetServicesBuilder] - Last build info error:', error);
            return;
        }
    } 
    // IF content is not null, compare the build info
    console.log('[NetServicesBuilder] - Last build info from cloud:', buildInfo);
     const lastBuildVersion = buildInfo.build; // 20250516.001
     const [lastBuildDate, lastBuildNumber] = lastBuildVersion.split('.');


    if (!this.projectVersion) {
      this.loadProjectVersion();
    }


     // Update build version
     const date = new Date();
     let count = 0;
     const yyyyMMdd = date.toISOString().slice(0, 10).replace(/-/g, '');
     if (this.projectVersion !=  buildInfo.version ||yyyyMMdd > lastBuildDate) {
        // Reset build number
        count = 1;
     } else {
        // Increment build number
        count = parseInt(lastBuildNumber) + 1;
     }
     const buildVersion = `${yyyyMMdd}.${String(count).padStart(3, '0')}`;
     this.tempBuildInfo = {
        version: this.projectVersion,
        build: buildVersion,
        commit: execSync('git rev-parse --short HEAD').toString().trim(),
        timestamp: new Date().toISOString(),
     }
     console.log('[NetServicesBuilder] - Next build info:', this.tempBuildInfo);
  }

  private async initBuildInfo() {
     // Init
     this.loadProjectVersion();
     const version = this.projectVersion;
     const commit = execSync('git rev-parse --short HEAD').toString().trim();
     const timestamp = new Date().toISOString(); 
 
     const date = new Date();
     const yyyyMMdd = date.toISOString().slice(0, 10).replace(/-/g, '');
 
     const count = 1;
     const buildNumber = `${yyyyMMdd}.${String(count).padStart(3, '0')}`;
     const buildInfo = {
        version,
        build: buildNumber,
        commit,
        timestamp,
      };
  
     writeFileSync(this.buildInfoFilePath, JSON.stringify(buildInfo));
     return buildInfo;
  }

  private loadProjectVersion() {
    const projectFile = path.join(`${this.config.services[0].source}`, `${this.config.services[0].executeFile}.csproj`);
    const projectContent = readFileSync(projectFile, 'utf8');
    const versionMatch = projectContent.match(/<Version>(.*?)<\/Version>/);
    this.projectVersion = versionMatch ? versionMatch[1] : '0.0.1';
  }
  private getPublishServiceName() {
    if(!this.tempBuildInfo) {
        return 'unknown';
    }
    return `common-services_${platform()}_${this.tempBuildInfo.version}_${this.tempBuildInfo.build}`;
  }

  private async testListVersions() {
    try {
        const bucket = S3_RELEASE_APP_BUCKET;
        const folder = 'released-files.042024/prod/luma/mac/versions';
        const result = await AwsS3Client.readSubFolderDir(bucket, folder);
        console.log('[NetServicesBuilder] - List object versions:', result);
    } catch (error) {
        console.error('[NetServicesBuilder] - List object versions error details:', {
            error: error.message,
            code: error.code,
            requestId: error.$metadata?.requestId,
            cfId: error.$metadata?.cfId
        });
    }

    
   
  }

  private async testService(service: NetServiceConfig, output: string) {
    console.log(
      `[NetServicesBuilder] - Test: Start test net service [${service.name}] ...`
    );
    // Check exists output directory
    if (!existsSync(output)) {
      throw new Error(
        'Output directory not found. Please check the output directory path.'
      );
    }

    const executable = path.join(output, `${service.executeFile}`);
    if (!existsSync(executable)) {
      throw new Error(
        'Executable file not found. Please check the executable file path.'
      );
    }
    console.log(
      `[NetServicesBuilder] - Test: Executable file: ${executable} ✅`
    );
    const processArgs = [
      `--service=uploader`,
      `env=dev`,
      `--instanceId=${v4()}`,
      `--appversion=1.1.2`,
      `--socket-server-host=127.0.0.1`,
      `--socket-server-port=40000`,
    ];

    const _process = spawn(executable, processArgs);
    _process.stdout.on('data', (data) => {
      console.log(data.toString());
    });
    _process.stderr.on('data', (data) => {
      console.error(data.toString());
    });
  }

  private async dotnetInvokeCommand(command: string, ...config: string[]) {
    let exe_command = `dotnet ${command} `;
    config.forEach((c) => {
      exe_command += `${c} `;
    });
    console.log(`[NetServicesBuilder] - dotnetInvokeCommand: ${exe_command}`);
    return executeCommand(exe_command);
  }

  private correactExeFile(exeFile: string) {
    if (platform() === 'win32') {
      return `${exeFile}.exe`;
    }
    return exeFile;
  }
}
