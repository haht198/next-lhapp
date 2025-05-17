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
  });
};
