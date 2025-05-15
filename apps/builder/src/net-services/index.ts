// Config

import { existsSync, mkdirSync } from "fs";
import path from "path";
import { platform, arch } from 'os';
import { exec, spawn } from "child_process";
import { executeCommand } from "../utils/shell";
import { dotnetCommans } from "./command";
import { v4 } from "uuid";
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
    private readonly macNetCoreEntitlementsFilePath = path.join(__dirname, '../../../../resources/mac-netcore.entitlements.plist');
    private readonly signServiceExtension = platform() === 'darwin' ? '.dylib' : '.dll';



    constructor(config: NetServicesBuildConfig) {
        this.config = config;
    }

    public async start() {
        console.log('[NetServicesBuilder] Start build net services with config:', {
            ...this.config,
            signServiceExtension: this.signServiceExtension,
            macNetCoreEntitlementsFilePath: this.macNetCoreEntitlementsFilePath,
            platform: platform(),
            arch: arch()
        });
        this.validateBuild(this.config);
        console.log('[NetServicesBuilder] - Start: Validate build configuration ✅');
        for (const service of this.config.services) {
          const result = await this.build(service, this.config.deployDir);
          if (!result) {
            throw new Error(`[NetServicesBuilder] - Start: Build failed with code ${result}`);
          }
          // Test service
          await this.testService(service, this.config.deployDir);
        }
    }

    public async build(service: NetServiceConfig, output: string) {
        console.log(`[NetServicesBuilder] - Build: Start build net service [${service.name}] ...`);
        // Check exists source file
        if (!existsSync(service.source)) {
            const errorMessage = `Source file: ${service.source} not found. Please check the source file path.`;
            console.error(errorMessage);
            throw new Error(errorMessage);
        }
        console.log(`[NetServicesBuilder] - Build: Source file: ${service.source} ✅`);

        // Check exists output directory    
        if (!existsSync(output)) {
            // Create output directory
            mkdirSync(output, { recursive: true });
        }
        console.log(`[NetServicesBuilder] - Build: Output directory: ${output} ✅`);

        // log command
        await executeCommand(`dotnet --info`);

       try {
        const restoreCode = await this.dotnetInvokeCommand(dotnetCommans.RESTORE, service.source);
        if (restoreCode !== 0) {
            throw new Error(`[NetServicesBuilder] - Build: Restore failed with code ${restoreCode}`);
        }
        console.log(`[NetServicesBuilder] - Build: Restore code: ${restoreCode} ✅`);

        const buildCode = await this.dotnetInvokeCommand(dotnetCommans.BUILD, service.source);
        if (buildCode !== 0) {
            throw new Error(`[NetServicesBuilder] - Build: Build failed with code ${buildCode}`);
        }
        console.log(`[NetServicesBuilder] - Build: Build code: ${buildCode} ✅`);

        const publishCode = await this.dotnetInvokeCommand(dotnetCommans.PUBLISH, service.source, 
            '-c Release',
            `-r ${platform() === 'win32' ? 'win-x64' : 'osx-arm64'}`, // todo check architecture
            '--self-contained=true',
            `-p:Version=1.1.1`,
            `-p:InformationalVersion=1.1.1`,
            `-p:AssemblyVersion=1.1.1`,
            `-o ${output}`
        );
          
        if (publishCode !== 0) {
            throw new Error(`[NetServicesBuilder] - Build: Publish failed with code ${publishCode}`);
        }
        console.log(`[NetServicesBuilder] - Build: Publish code: ${publishCode} ✅`);

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
            throw new Error('Deploy path is required. Please check the deployDir property.');
        }

        if (!existsSync(this.macNetCoreEntitlementsFilePath)) {
            throw new Error('Mac net core entitlements file not found. Please check the file path.');
        }
    }

    private async testService(service: NetServiceConfig, output: string) {
        console.log(`[NetServicesBuilder] - Test: Start test net service [${service.name}] ...`);
        // Check exists output directory    
        if (!existsSync(output)) {
            throw new Error('Output directory not found. Please check the output directory path.');
        }

        const executable = path.join(output, `${service.executeFile}`);
        if (!existsSync(executable)) {
            throw new Error('Executable file not found. Please check the executable file path.');
        }

        console.log(`[NetServicesBuilder] - Test: Executable file: ${executable} ✅`);
        const processArgs = [
            `--service=uploader`,
            `env=dev`,
            `--instanceId=${v4()}`,
            `--appversion=1.1.2`,
            `--socket-server-host=127.0.0.1`,
            `--socket-server-port=40000`,
        ]

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
        config.forEach(c => {
          exe_command += `${c} `;
        });
        console.log(`[NetServicesBuilder] - dotnetInvokeCommand: ${exe_command}`);
        return executeCommand(exe_command);
    }


   
    
}

