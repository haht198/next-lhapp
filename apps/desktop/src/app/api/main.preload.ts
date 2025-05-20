import { contextBridge, ipcRenderer } from 'electron';
import { electronCoreApi } from '@creative-force/electron-core/preload';

contextBridge.exposeInMainWorld('electron', {
  getAppVersion: () => ipcRenderer.invoke('get-app-version'),
  platform: process.platform,
  auth: {
    bootstrap: (client: 'hue' | 'luma' | 'ink') => ipcRenderer.invoke('bootstrap-auth', client),
    login: () => ipcRenderer.invoke('auth-login'),
  },
});

electronCoreApi();
