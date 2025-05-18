import {
  createWriteStream,
  existsSync,
  readdirSync,
  statSync,
  unlinkSync,
} from 'fs';
import * as path from 'path';
import * as yazl from 'yazl';
import * as yauzl from 'yauzl';
import * as rimraf from 'rimraf';
import * as mkdirp from 'mkdirp';

export class FsUtil {
  static zipFolder(sourceFolder: string, distFolder: string) {
    return new Promise((resolve, reject) => {
      if (existsSync(distFolder)) {
        unlinkSync(distFolder);
      }
      const zipper = new yazl.ZipFile();
      zipper.outputStream
        .pipe(createWriteStream(distFolder))
        .on('close', function () {
          resolve(true);
        });
      const files = this.getFilesInFolder(sourceFolder, sourceFolder);
      this.addZipFiles(files, zipper);
      zipper.end();
    });
  }

  static async unZipFiles(
    sourceZipFile: string,
    descFolder: string,
    log?: (msg: string, ...args: Array<any>) => void
  ): Promise<boolean> {
    if (!existsSync(sourceZipFile)) {
      return false;
    }
    if (existsSync(descFolder)) {
      rimraf.sync(descFolder);
    }
    if (!log) {
      log = (msg: string, ...args: Array<any>) => {
        console.log(msg, args);
      };
    }
    mkdirp.sync(descFolder);
    return new Promise<boolean>((resolve, reject) => {
      yauzl.open(sourceZipFile, { lazyEntries: true }, (err, zipfile) => {
        if (err) {
          resolve(false);
          log(`cannot upzip from ${sourceZipFile}`, err);
          return;
        }
        zipfile.once('end', function () {
          zipfile.close();
          resolve(true);
        });
        zipfile.readEntry();
        zipfile.on('entry', (entry) => {
          if (/\/$/.test(entry.fileName)) {
            zipfile.readEntry();
          } else {
            zipfile.openReadStream(entry, function (err, readStream) {
              if (err) {
                log(`unzip file error`, err);
                resolve(false);
                return;
              }
              readStream.on('end', function () {
                zipfile.readEntry();
              });
              const filePath = path.join(descFolder, entry.fileName);
              if (!existsSync(path.dirname(filePath))) {
                mkdirp.sync(path.dirname(filePath));
              }
              readStream.pipe(createWriteStream(filePath));
            });
          }
        });
      });
    });
  }
  static mkdir(folder: string) {
    if (!existsSync(folder)) {
      mkdirp.sync(folder);
    }
  }

  private static getFilesInFolder(folder: string, rootFolder: string): any {
    const result = {
      relativePath: folder.replace(rootFolder, '').replace('/', ''),
      files: [],
      folders: [],
    };
    const files = readdirSync(folder);
    files.forEach(async (f) => {
      const filePath = `${folder}/${f}`;
      if (statSync(filePath).isFile()) {
        result.files.push(filePath as never);
      } else {
        result.folders.push(
          this.getFilesInFolder(filePath, rootFolder) as never
        );
      }
    });
    return result;
  }

  private static addZipFiles(folder: any, zipper: any) {
    if (folder?.relativePath) {
      zipper.addEmptyDirectory(folder.relativePath);
    }
    folder.files.forEach((f: any) => {
      console.info(`zipped file: ${f}`);
      let relativePath = path.basename(f);
      if (folder?.relativePath) {
        relativePath = `${folder?.relativePath}/${relativePath}`;
      }
      zipper.addFile(f, relativePath, { compress: true });
    });
    if (folder?.folders && folder?.folders?.length > 0) {
      folder?.folders?.forEach((f: any) => {
        this.addZipFiles(f, zipper);
      });
    }
  }
}
