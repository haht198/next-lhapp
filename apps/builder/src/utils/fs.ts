import { createWriteStream, readdirSync, statSync } from 'fs';
import { unlinkSync } from 'fs';
import { existsSync } from 'fs';
import path from 'path';
import * as yazl from 'yazl';

export const zipFolder = async (sourceFolder: string, distFolder: string) => {
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
    const files = getFilesInFolder(sourceFolder, sourceFolder);
    addZipFiles(files, zipper);
    zipper.end();
  });
};

export const unzipFile = async (sourceFile: string, distFolder: string) => {
  return new Promise((resolve, reject) => {
    const unzip = new yazl.Unzip();
    unzip.on('error', (error) => {
      reject(error);
    });
    unzip.on('close', () => {
      resolve(true);
    });
    unzip.addFile(sourceFile, distFolder);
    unzip.end();
  });
};

const getFilesInFolder = (folder: string, rootFolder: string) => {
  const result = {
    relativePath: folder.replace(rootFolder, '').replace('/', ''),
    files: [],
    folders: [],
  };
  const files = readdirSync(folder);
  files.forEach(async (f) => {
    const filePath = `${folder}/${f}`;
    if (statSync(filePath).isFile()) {
      result.files.push(filePath);
    } else {
      result.folders.push(getFilesInFolder(filePath, rootFolder));
    }
  });
  return result;
};

const addZipFiles = (folder, zipper) => {
  if (folder.relativePath) {
    zipper.addEmptyDirectory(folder.relativePath);
  }
  folder.files.forEach((f) => {
    console.info(`zipped file: ${f}`);
    let relativePath = path.basename(f);
    if (folder.relativePath) {
      relativePath = `${folder.relativePath}/${relativePath}`;
    }
    zipper.addFile(f, relativePath, { compress: true });
  });
  if (folder.folders && folder.folders.length > 0) {
    folder.folders.forEach((f) => {
      addZipFiles(f, zipper);
    });
  }
};
