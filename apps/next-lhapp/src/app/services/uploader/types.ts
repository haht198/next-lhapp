export interface GetPresignedUrlRequest {
    clientId: string;
    files: Array<{
        localId: string;
        fileLength: number;
        fileName: string;
    }>;
    physicalFileTypeId: number;
}


export interface GetPresignedUrlResult<T, E> {
    headers: Record<string, string>;
    files: T[];
    errorFiles: E[];
}

export interface PresignedUrlAssetFile {
    localId: string;
    contentType: string;
    tempFileId: string;
    tempAssetId: string;
    presignedUrl: string;
}

export interface PresignedUrlErrorAssetFile {
    localId: string;
    assetId: string;
    errorCode: string;
    fileName: string;
}


export interface UploadBatchFilesPayload {
    id: string;
    headers: Record<string, string>;
    files: Array<{
        localId: string;
        contentType: string;
        localPath: string;
        presignedUrl: string;
        fileLength: number;
    }>;
}