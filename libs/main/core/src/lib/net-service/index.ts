import Logger from '@creative-force/eslogger';
import { app } from 'electron';
import {
  mkdirSync,
  readdirSync,
  readFileSync,
  unlinkSync,
  writeFileSync,
} from 'fs';
import { existsSync } from 'fs';
import { join } from 'path';
import { Application } from '../static';
import { RELEASE_SERVER_URL } from '@creative-force/electron-shared';
import { BrowserUtils } from '../utils/browser';
import { FsUtil } from '@next-lhapp/utils';
import * as rimraf from 'rimraf';
import { compareVersions } from '../utils/versioning';
import { NetServiceProcess, NetServiceProcessArgs } from './process';
import { v4 } from 'uuid';
import { SocketConfig } from './configs';
import { CFAppSocket } from '../socket/cf-socket';
import { CFAppSocketServer } from '../socket/socket-server';
interface NetServiceState {
  status:
    | 'INITIALIZED'
    | 'NOT_INSTALLED'
    | 'CHECKING_VERSION'
    | 'DOWNLOADING'
    | 'UNZIPPING'
    | 'INSTALLED'
    | 'RUNNING'
    | 'STOPPED';
  currentBuildInfo: BuildInfo | null;
  newBuildInfo: BuildInfo | null;
  isInstalled: boolean;
  processes: {
    [key: string]: NetServiceProcess;
  };
}

interface BuildInfo {
  version: string;
  build: string;
  timestamp: string;
}

class _NetService {
  public state: NetServiceState = {
    status: 'INITIALIZED',
    currentBuildInfo: null,
    newBuildInfo: null,
    isInstalled: false,
    processes: {},
  };

  private readonly INSTALL_SERVICES_FOLDER = join(
    app.getPath('userData'),
    'Creative Force',
    'services'
  );

  private readonly TEMP_DOWNLOAD_FOLDER = join(
    app.getPath('userData'),
    'Creative Force',
    'temp-download'
  );

  private lastDownloadEmitTime = 0;
  private readonly DOWNLOAD_THROTTLE_TIME = 1000; // 800ms
  private socketServer!: CFAppSocketServer;
  bootstrap() {
    Logger.info('[Core] - Net Service - Bootstrap net services...');
    // Check application installed services
    if (!existsSync(this.INSTALL_SERVICES_FOLDER)) {
      mkdirSync(this.INSTALL_SERVICES_FOLDER, { recursive: true });
      Logger.info(
        `[Core] Create install services folder: ${this.INSTALL_SERVICES_FOLDER}`
      );
      this.state.isInstalled = false;
    }
    // check installed services by is folder empty
    const installedServices = readdirSync(this.INSTALL_SERVICES_FOLDER);
    if (installedServices.length === 0) {
      Logger.info(
        '[Core] - Net Service - No installed services found. Do install services...'
      );
      this.state.isInstalled = false;
    } else {
      Logger.info(
        '[Core] - Net Service - Installed services found',
        installedServices
      );
      // Read build info
      this.state.currentBuildInfo = JSON.parse(
        readFileSync(
          join(this.INSTALL_SERVICES_FOLDER, 'build-info.json'),
          'utf8'
        )
      );  
      console.log('currentBuildInfo', this.state.currentBuildInfo);
      this.state.isInstalled = true;
    }
   
    // If service is installed, init socket server
    if (this.state.isInstalled) {
      this.initSocketServer();
      return;
    }

    if (this.state.status === 'NOT_INSTALLED') {
      // Install services and init socket server
      // TODO handler on app ready event
      setTimeout(() => {
        this.installServices();
        this.initSocketServer();
      }, 2000);
      return;
    }
  }

  async startProcess(identifier: string) {
    // Check socket server is started
    if (!this.socketServer) {
      await this.initSocketServer();
      if (!this.socketServer) {
        Logger.error('[Core] - Net Service - Socket server is not started');
        return;
      }
    }

    const _processArgs: NetServiceProcessArgs = {
      service: identifier,
      env: 'dev',
      instanceId: Application.instanceId,
      appVersion: this.state.currentBuildInfo?.version || '0.0.0',
      socket: {
        host: SocketConfig.host,
        port: SocketConfig.port,
      }
    }
    const process = new NetServiceProcess( join(this.INSTALL_SERVICES_FOLDER, 'dist', 'Common.Services'), _processArgs);
    const result = process.start();
    if (!result.isSuccess) {
      Logger.error(`[Core] - NetService - Start ${identifier} service failed.`, {
        result,
      });
      return;
    }
    this.state.processes[identifier] = process;
    // send change
    // listen event from socket server
    this.socketServer.listen('uploader', 'request-user-settings', (data) => {
      Logger.info('[Core] - Net Service - Request user settings received', data);
      const mockData = {
        userId: 'mock-user_'+ v4(),
        userName: 'HaHT',
        studioName: 'Creative Force',
        studioId: 'mock-studio_'+ v4(),
        workspace: '/Users/haht/Downloads',
      }
      this.socketServer.send('uploader', 'change-user-settings', mockData);
    });
    this.socketServer.listen('uploader', 'request-host-application-state', (data) => {
      Logger.info('[Core] - Net Service - Request host application state received', data);
      const mockData = {
        appName: app.getName(),
        appVersion: app.getVersion(),
      }
      this.socketServer.send('uploader', 'change-host-application-state', mockData);
    });
  }

  async testUpload() {
    if (!this.socketServer) {
      Logger.error('[Core] - Net Service - Socket server is not started');
      return;
    }
    // listen event from socket server
    this.socketServer.listen('uploader', 'upload-finished', (data) => {
      Logger.info('[Core] - Net Service - Test upload received', data);
    });
    // send event to socket server
    this.socketServer.send('uploader', 'upload-batch-files', {
      message: 'test upload',
    });
  }

  async stopProcess(identifier: string) {  
    const process = this.state.processes[identifier];
    if (process) {
      process.stop();
      delete this.state.processes[identifier];
    }
  }

  async checkVersion() {
    const result = {
      currentVersion: this.state.currentBuildInfo,
      newVersion: null,
    }
    try {
      Logger.info('[Core] - Net Service - Check new version...', {
        currentBuildInfo: this.state.currentBuildInfo,
      });
      // update state
      this.state.status = 'CHECKING_VERSION';
      this.emitState();
      // check temp download folder
      if (!existsSync(this.TEMP_DOWNLOAD_FOLDER)) {
        mkdirSync(this.TEMP_DOWNLOAD_FOLDER, { recursive: true });
        Logger.info(
          `[Core] Create temp download folder: ${this.TEMP_DOWNLOAD_FOLDER}`
        );
      }
      const downloadUrl = `https://download.creativeforce-dev.io/released-services/dev/mac/last-build-info.json`;
      const _downloader = BrowserUtils.downloadFile({
        url: downloadUrl,
        downloadFolder: join(this.TEMP_DOWNLOAD_FOLDER, 'last-build-info.json'),
      });
  
      if (!_downloader) {
        Logger.error('[Core] - Net Service - Failed to download last build info');
        return result;
      }
      if (!await _downloader.finish()) {
        Logger.error('[Core] - Net Service - Failed to download last build info');
        return result;
      }
      const newBuildInfo = JSON.parse(
        readFileSync(
          join(this.TEMP_DOWNLOAD_FOLDER, 'last-build-info.json'),
          'utf8'
        )
      );
      console.log('newBuildInfo', newBuildInfo);

      // compare build info
      const currentBuildInfo = this.state.currentBuildInfo || {
        version: '0.0.0',
        build: '0.0.0',
        timestamp: '0.0.0',
      };
      if (compareVersions(newBuildInfo.version, currentBuildInfo.version) > 0 || compareVersions(newBuildInfo.build, currentBuildInfo.build) > 0) {
        Logger.info(`[Core] - Check new version - New version: ${newBuildInfo.version}__${newBuildInfo.build}. Current version: ${currentBuildInfo.version}__${currentBuildInfo.build}`);
        this.state.newBuildInfo = newBuildInfo;
        this.emitState();
        result.newVersion = newBuildInfo;
        return result;
      }
      return result;

    } catch (error) {
      Logger.error('[Core] - Net Service - Failed to check new version', error);
      return result;
    }
  }

  async downloadNewVersion(buildInfo: BuildInfo) {
    buildInfo = buildInfo || this.state.newBuildInfo;
    if (!buildInfo) {
      Logger.error('[Core] - NetService - Download new version failed, build info is required.');
      return false;
    }
    Logger.info('[Core] - Net Service - Download new version...');
    try {
      // update state
      this.state.status = 'DOWNLOADING';
      this.emitState();

      // enrich download service url
      const downloadServiceUrl = `${RELEASE_SERVER_URL}released-services/dev/mac/versions/${buildInfo.version}/common-services_darwin_${buildInfo.version}_${buildInfo.build}.zip`;
      console.log('do download service url', downloadServiceUrl);
      // download service
      const downloadPath = join(
        this.TEMP_DOWNLOAD_FOLDER,
        `common-services_darwin_${buildInfo.version}_${buildInfo.build}.zip`
      );

      if (existsSync(downloadPath)) {
        Logger.info(`[Core] - Net Service - Download new version - Download path already exists, remove it. ${downloadPath}`);
        unlinkSync(downloadPath);
      }
      const startDownloadTime = Date.now();
      const downloadTask = BrowserUtils.downloadFile({
        url: downloadServiceUrl,
        downloadFolder: downloadPath,
      });
      if (!downloadTask) {
        Logger.error('[Core] - Net Service - Failed to download service');
        return;
      }
      downloadTask.onProgress = (progress) => {
        if (!progress) {
          return;
        }
        const { filename, receivedBytes, totalBytes } = progress;
        
        // Throttle emit state for download progress
        const now = Date.now();
        if (now - this.lastDownloadEmitTime < this.DOWNLOAD_THROTTLE_TIME && receivedBytes !== totalBytes) {
          return;
        }
        this.lastDownloadEmitTime = now;
        
        // update state
        const percentProgress = Math.round(receivedBytes / totalBytes * 100);
        this.state.status = percentProgress === 100 ? 'DOWNLOADED' : 'DOWNLOADING' + ' ' + percentProgress + '%' as any;
        this.emitState();
      };
      const downloadResult = await downloadTask.finish();
      const downLoadEndTime = Date.now();
      if (!downloadResult) {
        Logger.error('[Core] - Net Service - Failed to download service');
        return;
      }
      return {
        isSuccess: true,
        downloadPath,
        duration: (downLoadEndTime - startDownloadTime) / 1000 + 's', // convert to seconds
      };

    } catch (error) {
      Logger.error('[Core] - Net Service - Failed to download new version', error);
      return false;
    }
  }

  async installServices() {
    Logger.info('[Core] - Net Service - Install services...');
    
    const { newVersion } = await this.checkVersion();
    if (!newVersion) {
      Logger.error('[Core] - NetService - Install services failed, no new version found on server.');
      return;
    }

    const newVersionResult = await this.downloadNewVersion(newVersion) || {
      isSuccess: false,
      downloadPath: null,
      duration: null,
    };
 
    const { isSuccess, downloadPath } = newVersionResult;
    if (!isSuccess) {
      Logger.error('[Core] - NetService - Install services failed, failed to download new version.');
      return;
    }

    const installResult = await this.doInstallServices({
      downloadPath,
      buildInfo: newVersion,
    });
    if (!installResult) {
      Logger.error('[Core] - NetService - Install services failed, failed to install services.');
      return;
    }
    return true;
  }

  async doInstallServices({
    downloadPath,
    buildInfo,
  }: {
    downloadPath: string;
    buildInfo: BuildInfo;
  }) {
    try {
      if (!downloadPath || !existsSync(downloadPath)) {
        Logger.error('[Core] - NetService - Install services failed, download path is required.');
        return false;
      }
      // Clean install services folder
      Logger.info('[Core] - Net Service - Clean install services folder', this.INSTALL_SERVICES_FOLDER);
      rimraf.sync(this.INSTALL_SERVICES_FOLDER);
      // unzip service
      this.state.status = 'UNZIPPING';
      this.emitState();
      const unzipService = await FsUtil.unZipFiles(
        downloadPath,
        join(this.INSTALL_SERVICES_FOLDER, 'dist')
      );
      Logger.info('[Core] - Net Service - Unzip service result', unzipService);
      if (unzipService) {
        // copy build info to install services folder
        const buildInfoPath = join(
          this.INSTALL_SERVICES_FOLDER,
          'build-info.json'
        );
        writeFileSync(buildInfoPath, JSON.stringify(buildInfo, null, 2));
        Logger.info(
          '[Core] - Net Service - Copy build info to install services folder',
          buildInfoPath
        );
        // update state
        Logger.info('[Core] - Net Service - Update state to INSTALLED');
        this.state.status = 'INSTALLED';
        Application.emitApplicationState({
          netService: this.state,
        });
        return true;
      }
    } catch (error) {
      Logger.error('[Core] - NetService - Install services failed', error);
      return false;
    }
    return false;
  }

  uninstallServices() {
    // TODO Check services is running
    Logger.info('[Core] - Net Service - Uninstall services...');
    rimraf.sync(this.INSTALL_SERVICES_FOLDER);
    Logger.info('[Core] - Net Service - Uninstall services done');
  }

  private async initSocketServer() {
    Logger.info('[Core] - Net Service - Init socket server...');
    try {
      this.socketServer = await CFAppSocket.start({
        serverIdentifier: 'net-service',
        setting: {...SocketConfig} 
      })
      if (!this.socketServer) {
        Logger.error('[Core] - Net Service - Init socket server failed');
        return;
      }
      Logger.info('[Core] - Net Service - Socket server started', this.socketServer);
    } catch (error) {
      Logger.error('[Core] - Net Service - Init socket server failed', error);
    }
  }

  private getState() {
    return this.state;
  }

  private emitState() {
    Application.emitApplicationState({
      netService: this.state,
    });
  }
}

export const NetService = new _NetService();