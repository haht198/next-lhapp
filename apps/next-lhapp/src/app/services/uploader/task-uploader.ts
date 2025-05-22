import { TaskUploaderConfig, UploadFile } from './config';
import { ArrayUtils } from '@creative-force/electron-shared';
import { GetPresignedUrlRequest, UploadBatchFilesPayload } from './types';
export class TaskUploader {
  private _config: TaskUploaderConfig;
  constructor(config: TaskUploaderConfig) {
    this._config = config;
    // this._httpClient = this.createHttpClient();
    // console.log('TaskUploader', this);
    this._validatePreloadApi(); 
  }

  private _validatePreloadApi() {
    if (!window.electronUploader) {
      throw new Error('Preload api not found');
    }
    if (!window.electronUploader.bootstrap) {
      throw new Error('Preload api not found');
    }
    if (!window.electronUploader.uploadBatchFiles) {
      throw new Error('Preload api not found');
    }
    window.electronUploader.bootstrap().then((result) => {
      console.log('uploader bootstrap success', result);
    }).catch((error) => {
      console.error('uploader bootstrap error', error);
    });
  }

  async start(taskId: string) {
    try {
      console.log(`[TaskUploader] Start upload task ${taskId}`);
      // 1. Get files to upload
      const getFilesToUploadResponse = await this._config.getFilesToUpload(taskId);
      if (!getFilesToUploadResponse) {
        console.error(`[TaskUploader] No files to upload for task ${taskId}`);
        return;
      }
      console.log(`[TaskUploader] The task ${getFilesToUploadResponse.taskId} has ${getFilesToUploadResponse.files.length} files to upload`, getFilesToUploadResponse.files);
      // Validate files
      const fileInfo = await window.electronCore.fileSystem.getFileInfo(getFilesToUploadResponse.files.map((file) => file.localPath));
      if (fileInfo && fileInfo.length > 0) {
        const fileInfoMap = ArrayUtils.arrayToMap(fileInfo, 'path');
        // add file size to files
        getFilesToUploadResponse.files.forEach((file) => {
          file.fileLength = fileInfoMap[file.localPath]?.info?.size;
        }); 
      }
      console.log(`[TaskUploader] File info`, getFilesToUploadResponse);
      // 2. Get presigned url
      let presignedUrl = null;
      if (this._config.getPresignedUrl) {
        const _input: GetPresignedUrlRequest = {
            clientId: getFilesToUploadResponse.clientId,
            physicalFileTypeId: 2,
            files: getFilesToUploadResponse.files.map(f => {
                return {
                    localId: f.localId,
                    fileName: f.expectedOutputName || '',
                    fileLength: f.fileLength || 0,
                }
            }),
        }
        console.log(`[TaskUploader] Get presigned url input`, _input);
        presignedUrl = await this._config.getPresignedUrl(_input);
      }
      console.log(`[TaskUploader] Get presigned url ${presignedUrl}`);
      if (!presignedUrl) {
        console.error(`[TaskUploader] No presigned url found`);
        return;
      }
      // 3. Upload files
      const uploadFilesPayload: UploadBatchFilesPayload = {
        id: taskId,
        headers: presignedUrl.headers || {},
        files: getFilesToUploadResponse.files.map(f => {
          const presignedUrlFile = presignedUrl.files.find(p => p.localId === f.localId);
          if (!presignedUrlFile) {
            console.error(`[TaskUploader] No presigned url found for file ${f.localId}`);
            return null;
          }
          return {
            localId: f.localId,
            contentType: presignedUrlFile.contentType,
            localPath: f.localPath,
            presignedUrl: presignedUrlFile.presignedUrl,
            fileLength: f.fileLength || 0,
          }
        }).filter(f => f !== null),
      }
      console.log(`[TaskUploader] Upload files payload`, uploadFilesPayload);
      window.electronUploader.uploadBatchFiles(uploadFilesPayload);
      // prepare data
      // 4. Submit task
      const submitTask = await this._config.submitTask(taskId);
      console.log(`[TaskUploader] Submit task ${taskId}`, submitTask);
      return submitTask;
    } catch (error) {
      console.error(`[TaskUploader] Error uploading task ${taskId}`, error);
      throw error;
    }
  }
}
