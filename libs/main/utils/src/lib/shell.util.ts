import { exec, spawn } from 'child_process';

export const executeCommand = (
  command: string,
  dir?: string,
  stdoutReturn?: boolean
) => {
  return new Promise((resolve, reject) => {
    console.debug(`[Utils] Execute command "${command}" started...\n`);
    const processOption = dir ? { cwd: dir } : null;
    const process = exec(command, processOption);
    if (!process) {
      reject(new Error(`[Utils] Execute command "${command}" failed`));
      return;
    }
    let stdout = '';
    let stderr = '';
    process.stdout?.on('data', (data) => {
      console.debug(`${data.toString('utf8').trim()}`);
      stdout += `${data.toString('utf8').trim()}\n`;
    });
    process.stderr?.on('data', (data) => {
      console.debug(`${data.toString('utf8').trim()}`);
      stderr += `${data.toString('utf8').trim()}\n`;
    });
    process.on('exit', (code, signal) => {
      console.debug(`[Utils] Execute command "${command}" finished`, {
        code,
        signal,
      });
      setTimeout(() => {
        resolve(stdoutReturn ? stdout.trim() : code);
      }, 1000);
    });
    process.on('error', (err) => {
      console.debug(`[Utils] Execute command "${command}" error`, err);
      reject(stderr);
    });
  });
};

export const spawnCommand = (
  command: string,
  args: string[]
): Promise<number> => {
  return new Promise((resolve, reject) => {
    const signProcess = spawn(command, args, {
      shell: true,
      env: process.env,
    });
    signProcess.on('exit', (code, signal) => {
      console.debug(
        `[Utils] Spawn command "${command}" exit: ${code} ${signal}`
      );
      setTimeout(() => {
        resolve(code ?? 0);
      }, 1000);
    });
    signProcess.stdout.on('data', (data) => {
      console.debug(`${data.toString('utf8').trim()}`);
    });
    signProcess.stderr.on('data', (data) => {
      console.debug(
        `[Utils] Spawn command "${command}" stderr: ${data
          .toString('utf8')
          .trim()}`
      );
    });
  });
};
