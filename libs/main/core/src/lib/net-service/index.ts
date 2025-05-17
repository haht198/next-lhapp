import Logger from '@creative-force/eslogger';
import { app } from 'electron';
import { mkdirSync, readdirSync } from 'fs';
import { existsSync } from 'fs';
import { join } from 'path';

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
      this.installServices();
      this.initSocketServer();
      return;
    }
  }

  private static installServices() {
    Logger.info('[Core] - Net Service - Install services...');
  }

  private static initSocketServer() {
    Logger.info('[Core] - Net Service - Init socket server...');
  }
}
