import Logger from "@creative-force/eslogger";
import { spawn } from "child_process";
import { chmodSync, existsSync } from "fs";
import { platform } from "os";
import { v4 } from "uuid";

export interface NetServiceProcessArgs {
  service: string;
  env: string;
  instanceId: string;
  appVersion: string;
  socket: {
    host: string;
    port: number;
  }
}

export class NetServiceProcess {
  private identifier: string;
  private process: any;
  private readonly executePath: string;
  private readonly processArgs!: NetServiceProcessArgs;

  constructor(executePath: string, processArgs: NetServiceProcessArgs) {
    this.identifier = processArgs.service;
    this.executePath = platform() === 'darwin' ? executePath : executePath + '.exe';
    Logger.info(`[Core] - NetServiceProcess - Init ${this.identifier} service process`, {
      executePath: this.executePath,
    });
    this.processArgs = processArgs;
  }

  start() {
    // Check server i
    const result  = {
        isSuccess: false,
        error: '',
        process: null,
    }
    try {
        if (!this.executePath || !existsSync(this.executePath)) {
            result.error = `Execute path is required.`;
            return result;
        }
        if (!this.processArgs) {
            result.error = `Process args is required.`;
            return result;
        }
            
        // try chmod +x
        chmodSync(this.executePath, 0o755);

        // check process is running
        Logger.info(`[Core] - NetServiceProcess - Start ${this.identifier} service process...`); 
        const processArgs = [
            `--service=${this.processArgs.service}`,    
            `env=${this.processArgs.env}`,
            `--instanceId=${v4()}`,
            `--appversion=${this.processArgs.appVersion}`,
            `--socket-server-host=${this.processArgs.socket.host}`,
            `--socket-server-port=${this.processArgs.socket.port}`,
        ]
        this.process = spawn(this.executePath, processArgs);
        if (!this.process || !this.process.pid) {
            result.error = `Failed to start ${this.identifier} service process at ${this.executePath}. Process is null.`;
            Logger.error(`[Core] - NetServiceProcess - Cannot run ${this.identifier} service. Process is not available.`, {
                processArgs: processArgs,
                error: result.error,
            });
            return result;
        }
        this.process.on('error', (error: any) => {
            result.error = `Failed to start ${this.identifier} service process at ${this.executePath}. Process is null.`;
            Logger.error(`[Core] - NetServiceProcess - Run ${this.identifier} service failed.`, {
                processArgs: processArgs,
                result,
                error,
            });
            return result;
        });
        this.process.on('exit', (code: number, signal: string) => {
            result.error = `Process service ${this.identifier} exited with code ${code} and signal ${signal}.`;
            Logger.warn(`[Core] - NetServiceProcess - Run ${this.identifier} service with code ${code} and signal ${signal}.`, {
                processArgs: processArgs,
                result,
                code,
                signal,
            });
            return result;
        });
        this.process.on('close', (code: number, signal: string) => {
            result.error = `Process service ${this.identifier} close with code ${code} and signal ${signal}.`;
            Logger.warn(`[Core] - NetServiceProcess - Run ${this.identifier} service with code ${code} and signal ${signal}.`, {
                processArgs: processArgs,
                result,
                code,
                signal,
            });
            return result;
        });


        this.process.stdout.on('data', (data: any) => {
            console.log(data.toString());
          });
        this.process.stderr.on('data', (data: any) => {
            Logger.error(`[Core] - NetServiceProcess - Run ${this.identifier} service has error: ${data.toString()}`);
            console.error(data.toString());
          });
       
        result.isSuccess = true;
        result.process = this.process;
        return result;
    } catch (error) {
        Logger.error(`[Core] - NetServiceProcess - Start ${this.identifier} service process failed: ${error}`);
        this.process = null;
        return result;
    }
  }

  stop() {
    Logger.info(`[Core] - Net Service - Stop ${this.identifier} service process...`);
    try {
      if (this.process) {
        this.process.kill();
      }
    } catch (error) {
      Logger.error(`[Core] - NetServiceProcess - Stop ${this.identifier} service process failed: ${error}`);
    }
  }
  
}
