import { NetService } from './net-service';
import { Application } from './static';
import Logger, { LogConfig } from '@creative-force/eslogger';

export interface ApplicationConfig {
  appName: string;
  appPort: number;
}

export const bootstrapApplication = (config: ApplicationConfig) => {
  const _start = performance.now();
  Logger.info('[Core] Bootstrap application', config);
  LogConfig.useConsole();
  Application.initState();

  // Init net services
  NetService.bootstrap();
  const _end = performance.now();
  Application.isBootstrapped = true;
  Logger.info(`[Core] Bootstrap application time: ${_end - _start}ms`);
};

// Bootstrap application flow
