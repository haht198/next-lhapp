import Logger from '@creative-force/eslogger';
import { app } from 'electron';
import {
  chmodSync,
  mkdirSync,
  readdirSync,
  readFileSync,
  writeFileSync,
} from 'fs';
import { existsSync } from 'fs';
import { join } from 'path';
import { Application } from '../static';
import { RELEASE_SERVER_URL } from '@creative-force/electron-shared';
import { BrowserUtils } from '../utils/browser';
import { FsUtil } from '@next-lhapp/utils';
import * as rimraf from 'rimraf';

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

    // setTimeout(() => {
    //   // test unzip
    //   // remove install services folder
    //   console.log(
    //     'remove install services folder',
    //     this.INSTALL_SERVICES_FOLDER
    //   );
    //   rimraf.sync(this.INSTALL_SERVICES_FOLDER);
    //   const zipFilePath = `/Users/hafht/Library/Application Support/Electron/Creative Force/temp-download/common-services_darwin_1.0.0_20250517.002.zip`;
    //   FsUtil.unZipFiles(zipFilePath, this.INSTALL_SERVICES_FOLDER)
    //     .then((result) => {
    //       console.log('unzipService result', result);
    //     })
    //     .catch((error) => {
    //       console.log('unzipService error', error);
    //     });

    //   // this.installServices();
    //   // this.initSocketServer();
    // }, 2000);
    // return;

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

    // update state
    this.state.status = 'CHECKING_VERSION';
    this.emitState();

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
    // update state
    this.state.status = 'DOWNLOADING';
    this.emitState();
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
    this.state.status = 'UNZIPPING';
    this.emitState();
    const unzipService = await FsUtil.unZipFiles(
      join(
        this.TEMP_DOWNLOAD_FOLDER,
        `common-services_darwin_${buildInfo.version}_${buildInfo.build}.zip`
      ),
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
      // remove temp download folder
      Logger.info(
        '[Core] - Net Service - Remove temp download folder',
        this.TEMP_DOWNLOAD_FOLDER
      );
      rimraf.sync(this.TEMP_DOWNLOAD_FOLDER);
      // update state
      Logger.info('[Core] - Net Service - Update state to INSTALLED');
      this.state.status = 'INSTALLED';
      Application.emitApplicationState({
        netService: this.state,
      });
    }
  }

  private static initSocketServer() {
    Logger.info('[Core] - Net Service - Init socket server...');
  }

  private static emitState() {
    Application.emitApplicationState({
      netService: this.state,
    });
  }
}
