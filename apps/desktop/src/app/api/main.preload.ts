import { contextBridge, ipcRenderer } from 'electron';
import { electronCoreApi } from '@creative-force/electron-core/preload';

contextBridge.exposeInMainWorld('electron', {
  getAppVersion: () => ipcRenderer.invoke('get-app-version'),
  platform: process.platform,
});

electronCoreApi();
