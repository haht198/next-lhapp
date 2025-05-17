import Logger from '@creative-force/eslogger';
import { app } from 'electron';
import { chmodSync, mkdirSync, readdirSync, readFileSync } from 'fs';
import { existsSync } from 'fs';
import { join } from 'path';
import { Application } from '../static';
import { RELEASE_SERVER_URL } from '@creative-force/electron-shared';
import { BrowserUtils } from '../utils/browser';
import { unzip } from 'zlib';
import * as yazl from 'yazl'
interface NetServiceState {
  status: 'INITIALIZED' | 'NOT_INSTALLED' | 'INSTALLED' | 'RUNNING' | 'STOPPED';
}

export class NetService {
  public static state: NetServiceState = {
    status: 'INITIALIZED',
  };

  private static readonly INSTALL_SERVICES_FOLDER = join(
    app.getPath('userData'),
    'Creative Force',
    'services'
  );

  private static readonly TEMP_DOWNLOAD_FOLDER = join(
    app.getPath('userData'),
    'Creative Force',
    'temp-download'
  );

  static bootstrap() {
    Logger.info('[Core] - Net Service - Bootstrap net services...');
    // Check application installed services
    if (!existsSync(this.INSTALL_SERVICES_FOLDER)) {
      mkdirSync(this.INSTALL_SERVICES_FOLDER, { recursive: true });
      Logger.info(
        `[Core] Create install services folder: ${this.INSTALL_SERVICES_FOLDER}`
      );
    }
    // check installed services by is folder empty
    const installedServices = readdirSync(this.INSTALL_SERVICES_FOLDER);
    if (installedServices.length === 0) {
      Logger.info(
        '[Core] - Net Service - No installed services found. Do install services...'
      );
      this.state.status = 'NOT_INSTALLED';
    } else {
      Logger.info(
        '[Core] - Net Service - Installed services found',
        installedServices
      );
      this.state.status = 'INSTALLED';
    }

    // If service is installed, init socket server
    if (this.state.status === 'INSTALLED') {
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

  private static async installServices() {
    Logger.info('[Core] - Net Service - Install services...');
    // check temp download folder
    if (!existsSync(this.TEMP_DOWNLOAD_FOLDER)) {
      mkdirSync(this.TEMP_DOWNLOAD_FOLDER, { recursive: true });
      Logger.info(
        `[Core] Create temp download folder: ${this.TEMP_DOWNLOAD_FOLDER}`
      );
    }

    // // check permission temp download folder has write permission
    // // chmod write permission
    // chmodSync(this.TEMP_DOWNLOAD_FOLDER, 0o777);

    const downloadUrl = `https://download.creativeforce-dev.io/released-services/dev/mac/last-build-info.json`;
    const task = BrowserUtils.downloadFile({
      url: downloadUrl,
      downloadFolder: join(this.TEMP_DOWNLOAD_FOLDER, 'last-build-info.json'),
    });

    if (!task) {
      Logger.error('[Core] - Net Service - Failed to download last build info');
      return;
    }
    const result = await task.finish();
    if (!result) {
      Logger.error('[Core] - Net Service - Failed to download last build info');
      return;
    }
    const buildInfo = JSON.parse(
      readFileSync(
        join(this.TEMP_DOWNLOAD_FOLDER, 'last-build-info.json'),
        'utf8'
      )
    );
    console.log('buildInfo', buildInfo);
    // enrich download service url
    const downloadServiceUrl = `${RELEASE_SERVER_URL}released-services/dev/mac/versions/${buildInfo.version}/common-services_darwin_${buildInfo.version}_${buildInfo.build}.zip`;
    console.log('do download service url', downloadServiceUrl);
    // download service
    const serviceTask = BrowserUtils.downloadFile({
      url: downloadServiceUrl,
      downloadFolder: join(
        this.TEMP_DOWNLOAD_FOLDER,
        `common-services_darwin_${buildInfo.version}_${buildInfo.build}.zip`
      ),
    });
    if (!serviceTask) {
      Logger.error('[Core] - Net Service - Failed to download service');
      return;
    }
    const serviceResult = await serviceTask.finish();
    if (!serviceResult) {
      Logger.error('[Core] - Net Service - Failed to download service');
      return;
    }
    // unzip service
    const unzipService = await unzip(
      join(this.TEMP_DOWNLOAD_FOLDER, 'common-services.zip'),
      (error, result) => {
        if (error) {
          Logger.error('[Core] - Net Service - Failed to unzip service', error);
          return;
        }
        console.log('unzipService', result);
      }
    );
    console.log('unzipService', unzipService);
    // move service to install services folder
    const installServiceFolder = join(
      this.INSTALL_SERVICES_FOLDER,
      'common-services'
    );
    if (!existsSync(installServiceFolder)) {
      mkdirSync(installServiceFolder, { recursive: true });
      Logger.info(
        `[Core] Create install service folder: ${installServiceFolder}`
      );
    }
  }
  

  private static initSocketServer() {
    Logger.info('[Core] - Net Service - Init socket server...');
  }
}
