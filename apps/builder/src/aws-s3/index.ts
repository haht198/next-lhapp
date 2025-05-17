import {
  GetObjectCommand,
  ListObjectsCommand,
  PutObjectCommand,
  CopyObjectCommand,
  S3Client,
} from '@aws-sdk/client-s3';
import { createReadStream, readFile, readFileSync } from 'fs';
import { Readable } from 'stream';

export class AwsS3Client {
  static s3: S3Client;

  static async init() {
    if (!this.s3) {
      this.s3 = new S3Client();
    }
  }

  static async copyFile(bucket: string, sourceKey: string, targetKey: string) {
    try {
      const copyResult = await this.s3.send(
        new CopyObjectCommand({
          Bucket: bucket,
          CopySource: `${bucket}/${sourceKey}`,
          Key: targetKey,
        })
      );
      return copyResult;
    } catch (error) {
      console.warn(
        `[AwsS3Client] - Can not copy file: ${sourceKey} to ${targetKey} in bucket: ${bucket}`,
        {
          error: error.message,
          errorType: error.name,
          code: error.code,
          requestId: error.$metadata?.requestId,
          cfId: error.$metadata?.cfId,
        }
      );
    }
  }
  static async uploadFile(bucket: string, key: string, filePath: string) {
    try {
      const uploadResult = await this.s3.send(
        new PutObjectCommand({
          Bucket: bucket,
          Key: key,
          Body: readFileSync(filePath),
        })
      );
      // Check if the upload was successful
      // If the upload was successful, the ETag will be returned
      if (uploadResult && uploadResult.ETag && uploadResult.ETag.length > 0) {
        return true;
      }
      return false;
    } catch (error) {
      console.warn(
        `[AwsS3Client] - Can not upload file: ${filePath} to bucket: ${bucket}`,
        {
          error: error.message,
          errorType: error.name,
          code: error.code,
          requestId: error.$metadata?.requestId,
          cfId: error.$metadata?.cfId,
        }
      );
      return null;
    }
  }

  static async uploadStreamFile(bucket: string, key: string, filePath: string) {
    try {
      const stream = createReadStream(filePath);
      const uploadResult = await this.s3.send(
        new PutObjectCommand({
          Bucket: bucket,
          Key: key,
          Body: stream,
        })
      );
      // Check if the upload was successful
      // If the upload was successful, the ETag will be returned
      if (uploadResult && uploadResult.ETag && uploadResult.ETag.length > 0) {
        return true;
      }
      return false;
    } catch (error) {
      console.warn(
        `[AwsS3Client] - Can not upload file: ${filePath} to bucket: ${bucket}`,
        {
          error: error.message,
          errorType: error.name,
          code: error.code,
          requestId: error.$metadata?.requestId,
          cfId: error.$metadata?.cfId,
        }
      );
      return null;
    }
  }

  static async readFile(bucket: string, key: string) {
    try {
      const stream = await this.s3.send(
        new GetObjectCommand({
          Bucket: bucket,
          Key: key,
        })
      );
      const content = await this.streamToString(stream.Body as Readable);
      return { content, error: null };
    } catch (error) {
      console.warn(
        `[AwsS3Client] - Can not read file: ${key} from bucket: ${bucket}`,
        {
          error: error.message,
          errorType: error.name,
          code: error.code,
          requestId: error.$metadata?.requestId,
          cfId: error.$metadata?.cfId,
        }
      );
      return { content: null, error: error };
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
    const listRequest = await this.s3.send(
      new ListObjectsCommand({
        Bucket: bucket,
        Prefix: `${folder.replace(/\\/g, '/')}/`,
        Delimiter: '/',
      })
    );
    if (
      !listRequest ||
      !listRequest.CommonPrefixes ||
      listRequest.CommonPrefixes.length === 0
    ) {
      return [];
    }
    return listRequest.CommonPrefixes.map((t) => t.Prefix);
  }

  static async readSubFolderDir(bucket: string, remoteFolder: string) {
    if (!this.s3) {
      await this.init();
    }
    const listRequest = await this.s3.send(
      new ListObjectsCommand({
        Bucket: bucket,
        Prefix: `${remoteFolder.replace(/\\/g, '/')}/`,
        Delimiter: '/',
      })
    );
    if (
      !listRequest ||
      !listRequest.CommonPrefixes ||
      listRequest.CommonPrefixes.length === 0
    ) {
      return [];
    }
    return listRequest.CommonPrefixes.map((t) => t.Prefix);
  }
}
