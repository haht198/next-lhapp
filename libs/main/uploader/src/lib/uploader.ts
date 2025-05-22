import { app, ipcMain, webContents, WebContents } from "electron";
import { ICFAppModule, UPLOADER_IPC_EVENTS } from "@creative-force/electron-shared";
import { NetService } from "@creative-force/electron-core";
import Logger from "@creative-force/eslogger";
import { v4 } from "uuid";


 class _Uploader implements ICFAppModule {

  private _isStaredServices = false;
  readonly uploaderServiceIdentifier = 'uploader';
  private _uploaderIntegration: {
    listen: (event: string, callback: (data: any) => void) => void;
    send: (event: string, data: any) => void;
  } | null = null;
  private _uploaderProcess: any | null = null;
  private _webContents: WebContents | null = null;
  bootstrap(payload: { webContent: WebContents}) {
    console.log('uploader bootstrap');
    this._webContents = payload.webContent;
    this.handleUploaderEvents();
  }

  private async _init() {
    // resolve 
    if(this._isStaredServices) {
      return;
    }
    if (!NetService.state.isInstalled) {
      console.error('Init upload service error. Netservices is not instaall')
      return;
    }
    this._isStaredServices = !!NetService.state.processes[this.uploaderServiceIdentifier];
   
    console.info('[UploaderMain] Check upload net service', this._isStaredServices)
    if (!this._isStaredServices) {
      // start uploader service
     try {
      const res = await NetService.startProcess(this.uploaderServiceIdentifier, true);
      console.log('[UploaderMain] start uploader service', res);
      if (!res?.process || !res?.integration) {
        throw new Error('Init upload service error. Start uploader service failed')
      }
      this._uploaderProcess = res.process;
      this._uploaderIntegration = res.integration;
      this._isStaredServices = true;
      this._listenUploaderEvents(res);
      return {
        isSuccess: true,
      };

     } catch (error) {
      console.error('Init upload service error. Start uploader service failed', error)
      return {
        isSuccess: false,
        error: error
      };
     }
    }
  }

  private _listenUploaderEvents(res: any) {
    if (!this._uploaderIntegration) {
      return;
    }
    // listen event from socket server
    this._uploaderIntegration.listen('request-user-settings', (data) => {
    Logger.info('[Core] - Net Service - Request user settings received', data);
    const mockData = {
      userId: 'mock-user_'+ v4(),
      userName: 'HaHT',
      studioName: 'Creative Force',
      studioId: 'mock-studio_'+ v4(),
      workspace: '/Users/haht/Downloads',
    }
    this._uploaderIntegration?.send('change-user-settings', mockData);
  });
this._uploaderIntegration.listen('request-host-application-state', (data) => {
  Logger.info('[Core] - Net Service - Request host application state received', data);
  const mockData = {
    appName: app.getName(),
    appVersion: app.getVersion(),
  }
    this._uploaderIntegration?.send('change-host-application-state', mockData);
});

    this._uploaderIntegration.listen('upload-finished', (data) => {
      console.log('upload-finished', data);
    });
  }

  private handleUploaderEvents() {
    ipcMain.handle(UPLOADER_IPC_EVENTS.BOOTSTRAP, (event, payload) => this._init());
    ipcMain.on(UPLOADER_IPC_EVENTS.UPLOAD_BATCH_FILES, (event, payload) => {
      console.log('upload-batch-files', payload);
      const applicationState = NetService.state;
      // check uploader services
      if (!applicationState.processes[this.uploaderServiceIdentifier] || !this._uploaderIntegration) {
        console.error('Uploader services is not started');
        return;
      }
      // send event to uploader services
      this._uploaderIntegration.send('upload-batch-files', payload);  
    });
  }

  private uploadBatchFiles(payload: any) {
    if (!this._uploaderIntegration) {
      return;
    }
    console.log('upload-batch-files', payload);
      // listen event from socket server
     
      // send event to socket server
      this._uploaderIntegration.send('upload-batch-files', {
        message: 'test upload',
      });
  }

}
export const Uploader = new _Uploader();
