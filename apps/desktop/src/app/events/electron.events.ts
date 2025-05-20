/**
 * This module is responsible on handling all the inter process communications
 * between the frontend to the electron backend.
 */

import { app, ipcMain } from 'electron';
import { environment } from '../../environments/environment';
import { AuthService } from '../services/auth';

export default class ElectronEvents {
  static bootstrapElectronEvents(): Electron.IpcMain {
    return ipcMain;
  }
}

// Retrieve app version
ipcMain.handle('get-app-version', (event) => {
  console.log(`Fetching application version... [v${environment.version}]`);

  return environment.version;
});

// Handle App termination
ipcMain.on('quit', (event, code) => {
  app.exit(code);
});

ipcMain.handle('bootstrap-auth', (_, client: 'hue' | 'luma' | 'ink') => {
  try {
    AuthService.bootstrap(client)
    return true;
  } catch (error) {
    console.error('Error bootstrapping auth', error);
    return false;
  }
});

ipcMain.handle('auth-login', async (_) => {
  return AuthService.login();
});
