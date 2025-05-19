import Logger from '@creative-force/eslogger';
import { app, BrowserWindow, session, Session, WebContents } from 'electron';
import { existsSync, mkdirSync } from 'fs';
import { join } from 'path';
import { Application } from '../static';

export interface DownloadFileParams {
  url: string;
  downloadFolder?: string;
}

export interface IDownloadFileProcesses {
  finish?: () => Promise<boolean>;
  onProgress?: (progress: {
    filename: string;
    receivedBytes: number;
    totalBytes: number;
  }) => void;
  onError?: (progress: { filename: string; state: string }) => void;
}
export class DownloadFileProcesses implements IDownloadFileProcesses {
  private url: string;
  private downloadFolder: string;
  private webContent: WebContents;
  private _promise!: Promise<boolean>;

  onProgress?: (progress: {
    filename: string;
    receivedBytes: number;
    totalBytes: number;
  }) => void;

  constructor(
    params: DownloadFileParams & {
      webContent: WebContents;
    }
  ) {
    this.url = params.url;
    this.downloadFolder = params.downloadFolder || app.getPath('downloads');
    this.webContent = params.webContent;
    this.start();
  }

  finish(): Promise<boolean> {
    if (!this._promise) {
      this._promise = this.start();
    }
    return this._promise;
  }

  private start(): Promise<boolean> {
    if (this._promise) {
      return this._promise;
    }
    this._promise = new Promise((resolve, reject) => {
      try {
        this.webContent.downloadURL(this.url);
        // setup download listener
        this.webContent.session.on('will-download', (event, item) => {
          item.setSavePath(this.downloadFolder);
          // caculate progress percentage
          let progress = 0;
          item.on('updated', (_, state) => {
            if (state === 'progressing') {
              progress =
                (item.getReceivedBytes() / item.getTotalBytes()) * 100;
              this.onProgress?.({
                filename: item.getFilename(),
                receivedBytes: item.getReceivedBytes(),
                totalBytes: item.getTotalBytes(),
              });
            }
          });

          item.once('done', (_, state) => {
            // log done
            console.log('download done', state);
            if (state === 'completed') {
              resolve(true);
            } else {
              reject(new Error('Download failed'));
            }
          });
        });
      } catch (error) {
        reject(error);
      }
    });
    return this._promise;
  }

  private static setupDownloadListener(session: Session, options: any) {
    session.on('will-download', (event, item) => {
      const filename = item.getFilename();
      const savePath = options.getSavePath
        ? options.getSavePath(filename)
        : require('path').join(require('os').tmpdir(), filename);

      item.setSavePath(savePath);

      item.on('updated', (_, state) => {
        if (state === 'progressing') {
          if (!item.isPaused()) {
            options.onProgress?.({
              filename,
              receivedBytes: item.getReceivedBytes(),
              totalBytes: item.getTotalBytes(),
            });
          }
        }
      });

      item.once('done', (_, state) => {
        if (state === 'completed') {
          options.onDone?.({ filename, savePath });
        } else {
          options.onFail?.({ filename, state });
        }
      });
    });
  }
}

export class BrowserUtils {
  private static getMainWindow() {
    if (!Application.getMainWindow()) {
      Logger.warn('[Core] - BrowserUtils - No main window found');
      return;
    }
    return Application.getMainWindow();
  }

  static downloadFile(params: { url: string; downloadFolder?: string }) {
    if (!params.downloadFolder) {
      params.downloadFolder = app.getPath('downloads');
    }
    console.log('downloadFile', params.url, params.downloadFolder);

    const mainWindow = this.getMainWindow();
    if (!mainWindow) {
      Logger.warn('[Core] - BrowserUtils - No main window found');
      return;
    }
    return new DownloadFileProcesses({
      url: params.url,
      downloadFolder: params.downloadFolder,
      webContent: mainWindow.webContents,
    });
  }
}
