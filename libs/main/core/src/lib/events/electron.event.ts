import { ipcMain } from 'electron';
import { API_EVENTS } from '@creative-force/electron-core/preload';
import { Application } from '../static/application';
import { NetService } from '../net-service';
import { FileSystemUtils } from '../utils/file-system';

export const bootstrapElectronEvents = () => {
  return true;
};

ipcMain.handle(API_EVENTS.GET_APPLICATION_STATE, () => {
  return {
    isBootstrapped: Application.isBootstrapped,
    bootstrapTime: Application.bootstrapTime + 'ms',
    netService: NetService.state,
  };
});

ipcMain.handle(API_EVENTS.NET_SERVICE.INSTALL, () => {
  return NetService.installServices();
});

ipcMain.handle(API_EVENTS.NET_SERVICE.UNINSTALL, () => {
  return NetService.uninstallServices();
});

ipcMain.handle(API_EVENTS.NET_SERVICE.CHECK_VERSION, () => {
  return NetService.checkVersion();
}); 

ipcMain.handle(API_EVENTS.NET_SERVICE.DOWNLOAD, (_, buildInfo: any) => {
  return NetService.downloadNewVersion(buildInfo);
});


ipcMain.handle(API_EVENTS.NET_SERVICE.START, (_, service: string) => {
  return NetService.startProcess(service);
});


ipcMain.handle(API_EVENTS.NET_SERVICE.STOP, (_, service: string) => {
  return NetService.stopProcess(service);
});

ipcMain.handle(API_EVENTS.NET_SERVICE.TEST_UPLOAD, () => {
  return NetService.testUpload();
});

// File system
ipcMain.handle(API_EVENTS.FILE_SYSTEM.GET_FILE_INFO, (_, paths: string[]) => {
  return FileSystemUtils.getBatchFilesInfo(paths);
});


