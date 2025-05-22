import { CFRendererIPCEvent } from '@creative-force/electron-core/preload';

export interface ElectronCore {
  getApplicationState(): Promise<ApplicationState>;
  onApplicationStateChange(
    callback: (state: ApplicationState) => void
  ): CFRendererIPCEvent;
  fileSystem: {
    getFileInfo: (paths: string[]) => Promise<FileInfo[]>;
  };
}

export interface FileInfo {
  path: string;
  exists: boolean;
  info: {
    size: number;
    isFile: boolean;
    isDirectory: boolean;
    createdAt: Date;
    updatedAt: Date;
    ext: string;
    name: string;
  };
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
    electronUploader: {
      bootstrap: () => Promise<void>;
      uploadBatchFiles: (payload: any) => void
    };
  }
}
