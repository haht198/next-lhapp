import { ipcMain } from 'electron';
import { API_EVENTS } from '@creative-force/electron-core/preload';
import { Application } from '../static/application';
import { NetService } from '../net-service';

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
