
import { GetObjectCommand, ListObjectsCommand, PutObjectCommand, S3Client } from '@aws-sdk/client-s3';
import { createReadStream } from 'fs';
import { Readable } from 'stream';


export class AwsS3Client {
    static s3: S3Client;

    static async init() {
        if (!this.s3) {
            this.s3 = new S3Client();
        }
    }

    static async uploadFile(bucket: string, key: string, filePath: string) {
        if (!this.s3) {
            await this.init();
        }
        const stream = createReadStream(filePath);
        const uploadResult = await this.s3.send(new PutObjectCommand({
            Bucket: bucket,
            Key: key,
            Body: stream
        }));
        return uploadResult;
    }

    static async readFile(bucket: string, key: string) {
        try {
            const stream = await this.s3.send(new GetObjectCommand({
                Bucket: bucket,
                Key: key
            }));
            const content = await this.streamToString(stream.Body as Readable);
            return {content, error: null};
        } catch (error) {
            console.warn(`[AwsS3Client] - Can not read file: ${key} from bucket: ${bucket}`, {
                error: error.message,
                errorType: error.name,
                code: error.code,
                requestId: error.$metadata?.requestId,
                cfId: error.$metadata?.cfId
            });
            return {content: null, error: error};
        }
    }
    static async streamToString(stream: Readable): Promise<string> {
        return new Promise((resolve, reject) => {
          const chunks: Buffer[] = [];
          stream.on('data', (chunk) => chunks.push(chunk));
          stream.on('error', reject);
          stream.on('end', () => resolve(Buffer.concat(chunks).toString('utf-8')));
        });
      }

    static async readBucketFolder(bucket: string, folder: string) {
        if (!this.s3) {
            await this.init();
        }
        const listRequest = await this.s3.send(new ListObjectsCommand({
            Bucket: bucket,
            Prefix: `${folder.replace(/\\/g, '/')}/`,
            Delimiter: '/'
        }));
        if (!listRequest || !listRequest.CommonPrefixes || listRequest.CommonPrefixes.length === 0) {
            return [];
        }
        return listRequest.CommonPrefixes.map(t => t.Prefix);
    }
  

    static async readSubFolderDir(bucket: string, remoteFolder: string) {
        if (!this.s3) {
            await this.init();
        }
        const listRequest = await this.s3.send(new ListObjectsCommand({
            Bucket: bucket,
            Prefix: `${remoteFolder.replace(/\\/g, '/')}/`,
            Delimiter: '/'
          }));
          if (!listRequest || !listRequest.CommonPrefixes || listRequest.CommonPrefixes.length === 0) {
            return [];
          }
          return listRequest.CommonPrefixes.map(t => t.Prefix);
    }
}
