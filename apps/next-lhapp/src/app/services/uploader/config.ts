import { GetPresignedUrlRequest, GetPresignedUrlResult, PresignedUrlAssetFile, PresignedUrlErrorAssetFile } from './types';
export interface TaskUploaderConfig {
    clientId: ClientID;
    getFilesToUpload: (taskId: string) => Promise<GetFilesToUploadResponse | null>;
    getPresignedUrl: (payload: GetPresignedUrlRequest) => Promise<GetPresignedUrlResult<PresignedUrlAssetFile, PresignedUrlErrorAssetFile> | null>;
    submitTask: (payload: any) => Promise<unknown>;

}



export interface GetFilesToUploadResponse {
    taskId: string;
    clientId: string;
    files: UploadFile[];
}

export interface UploadFile {
    localId: string;
    localPath: string;
    fileLength?: number;
    expectedOutputName?: string;
}

export type ClientID = 'hue' |'luma'

export type WorkflowID = 'internal-post-production' | 'digital-processing'


export const httpClientConfigs = {
    'hue': {
        appId: '6',
        screenId: '5300',
    },
    'luma': {
        appId: '22',
        screenId: '5900',
    }
}