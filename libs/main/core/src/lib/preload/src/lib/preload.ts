import { contextBridge, ipcRenderer } from 'electron';
import { API_EVENTS } from './events';
import { RendererIPC } from './ipc-renderer';

export const electronCoreApi = () => {
  contextBridge.exposeInMainWorld('electronCore', {
    getApplicationState: () =>
      ipcRenderer.invoke(API_EVENTS.GET_APPLICATION_STATE),
    onApplicationStateChange: (callback: (state: any) => void) => {
      return RendererIPC.on(API_EVENTS.CHANGE_APPLICATION_STATE, callback);
    },
    netService: {
      install: () => ipcRenderer.invoke(API_EVENTS.NET_SERVICE.INSTALL),
      uninstall: () => ipcRenderer.invoke(API_EVENTS.NET_SERVICE.UNINSTALL),
      checkVersion: () => ipcRenderer.invoke(API_EVENTS.NET_SERVICE.CHECK_VERSION),
      download: (buildInfo: any) => ipcRenderer.invoke(API_EVENTS.NET_SERVICE.DOWNLOAD, buildInfo),
      start: (service: string) => ipcRenderer.invoke(API_EVENTS.NET_SERVICE.START, service),
      stop: (service: string) => ipcRenderer.invoke(API_EVENTS.NET_SERVICE.STOP, service),
      testUpload: () => ipcRenderer.invoke(API_EVENTS.NET_SERVICE.TEST_UPLOAD),
    },
  });
};
