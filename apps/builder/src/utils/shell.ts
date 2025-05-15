import { exec } from "child_process";

export const executeCommand =  (command: string, dir?: string, stdoutReturn?: boolean) => {
    return new Promise((resolve, reject) => {
      console.debug(`[Utils] Execute command "${command}" started...\n`);
      const processOption = dir ? { cwd: dir } : null;
      const process = exec(command, processOption);
      let stdout = '';
      let stderr = '';
      process.stdout.on('data', (data) => {
        console.debug(`${data.toString('utf8').trim()}`);
        stdout += `${data.toString('utf8').trim()}\n`;
      });
      process.stderr.on('data', (data) => {
        console.debug(`${data.toString('utf8').trim()}`);
        stderr += `${data.toString('utf8').trim()}\n`;
      });
      process.on('exit', (code, signal) => {
        console.debug(`[Utils] Execute command "${command}" finished`, { code, signal })
        setTimeout(() => { resolve(stdoutReturn ? stdout.trim() : code) }, 1000);
      });
      process.on('error', (err) => {
        console.debug(`[Utils] Execute command "${command}" error`, err);
        reject(stderr);
      });
    });
  }