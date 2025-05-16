import { ListObjectsCommand, S3Client } from '@aws-sdk/client-s3';
import S3 from 'aws-sdk/clients/s3'
import path from 'path';


export const listVersions = async (bucket: string, key: string) => {
    try {
        const s3Client = new S3();
        console.log(`[Aws-S3] List versions for ${bucket} and ${key}`);
        const allVersionFolders = await readSubFoldersDir(bucket, key, s3Client);
        return allVersionFolders && allVersionFolders.length > 0 ? allVersionFolders.map(v => path.basename(v)) : [];
      } catch (err) {
        console.error(`[Aws-S3] List versions has exception`, err);
        return [];
      }
}

export const listVersionsV3 = async (bucket: string, key: string) => {
    try {
        const s3Client = new S3Client();
        console.log(`[Aws-S3] List versions for ${bucket} and ${key}`);
        const allVersionFolders = await readSubFolderDirV3(bucket, key, s3Client);
        return allVersionFolders && allVersionFolders.length > 0 ? allVersionFolders.map(v => path.basename(v)) : [];
      } catch (err) {
        console.error(`[Aws-S3] List versions has exception`, err);
        return [];
      }
}   

const readSubFoldersDir = async (bucket: string, remoteFolder: string, s3Client: S3) => {
    const listRequest = await s3Client.listObjects({
      Bucket: bucket,
      Prefix: `${remoteFolder.replace(/\\/g, '/')}/`,
      Delimiter: '/'
    }).promise();
    if (!listRequest || !listRequest.CommonPrefixes || listRequest.CommonPrefixes.length === 0) {
      return [];
    }
    return listRequest.CommonPrefixes.map(t => t.Prefix);
  }

const readSubFolderDirV3 = async (bucket: string, key: string, s3Client: S3Client) => {
    const listRequest = await s3Client.send(new ListObjectsCommand({
        Bucket: bucket,
        Prefix: `${key.replace(/\\/g, '/')}/`,
        Delimiter: '/'
    }));
    if (!listRequest || !listRequest.CommonPrefixes || listRequest.CommonPrefixes.length === 0) {
        return [];
      }
      return listRequest.CommonPrefixes.map(t => t.Prefix);
}