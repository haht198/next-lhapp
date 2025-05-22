import Logger from '@creative-force/eslogger';
import * as fs from 'fs/promises';
import * as path from 'path';

export class FileSystemUtils {
    static async getBatchFilesInfo(paths: string[]) {
      try {
        const filesInfo = await Promise.all(
          paths.map(async (filePath) => {
            try {
              await fs.access(filePath); // Kiểm tra tồn tại
              const stat = await fs.stat(filePath);
              return {
                path: filePath,
                exists: true,
                info: {
                  size: stat.size,
                  isFile: stat.isFile(),
                  isDirectory: stat.isDirectory(),
                  createdAt: stat.birthtime,
                  updatedAt: stat.mtime,
                  ext: path.extname(filePath),
                  name: path.basename(filePath),
                },
              };
            } catch {
              return {
                path: filePath,
                exists: false,
                info: null,
              };
            }
          })
        );
        return filesInfo;
      } catch (error) {
        Logger.error('[FileSystemUtils] Error getting file info', error);
        return null;
      }
    }
  }