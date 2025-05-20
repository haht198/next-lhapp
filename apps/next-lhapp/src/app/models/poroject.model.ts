import { CFRendererIPCEvent } from '@creative-force/electron-core/preload';

export interface ElectronCore {
  getApplicationState(): Promise<ApplicationState>;
  onApplicationStateChange(
    callback: (state: ApplicationState) => void
  ): CFRendererIPCEvent;
}

export interface ApplicationState {
  isReady?: boolean;
  version?: string;
  platform?: string;
  bootstrapTime: number;
  [key: string]: any;
}

export interface ElectronAPI {
  auth: {
    bootstrap: (client: 'hue' | 'luma' | 'ink') => Promise<boolean>;
    login: () => Promise<any>;
  };
}

declare global {
  interface Window {
    electronCore: ElectronCore;
    electron: ElectronAPI;
  }
}
