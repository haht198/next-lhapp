import { ipcRenderer } from 'electron';

export interface CFRendererIPCEvent {
  unsubscribe: () => void;
  unsubscribeAll: () => void;
}

export class RendererIPC {
  static on(channel: string, cb: (...args: any[]) => void): CFRendererIPCEvent {
    const _handler = (_: Electron.IpcRendererEvent, ...args: any[]) =>
      cb(...args);
    ipcRenderer.on(channel, _handler);
    return {
      unsubscribe: () => {
        ipcRenderer.removeListener(channel, _handler);
      },
      unsubscribeAll: () => {
        ipcRenderer.removeAllListeners(channel);
      },
    };
  }
  static getListeners(channel: string) {
    return ipcRenderer.listenerCount(channel);
  }
}
