/**
 * Description: Store to share data
 */

import { API_EVENTS } from '@creative-force/electron-core/preload';
import { BrowserWindow } from 'electron';

export class Application {
  static get netServices() {
    return {};
  }

  static isBootstrapped = false;
  static bootstrapTime = 0;

  static initState() {
    console.log('[Core] Init application state...');
  }

  private static mainWindow: BrowserWindow;
  static setMainWindow(window: BrowserWindow) {
    Application.mainWindow = window;
  }
  static getMainWindow() {
    return Application.mainWindow;
  }

  static emitApplicationState(state: any) {
    console.log('[Core] Emit application state...', state);
    // emit ipc event to all windows
    // get all windows
    const windows = BrowserWindow.getAllWindows();
    windows.forEach((window) => {
      if (window.isDestroyed()) return;
      window.webContents.send(API_EVENTS.CHANGE_APPLICATION_STATE, state);
    });
  }
}
