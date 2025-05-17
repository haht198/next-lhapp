import { NetService } from './net-service';
import { Application } from './static';
import Logger, { LogConfig } from '@creative-force/eslogger';
import { bootstrapElectronEvents } from './events/electron.event';

export interface ApplicationConfig {
  appName: string;
  appPort: number;
}

export const bootstrapApplication = (config: ApplicationConfig) => {
  const _start = performance.now();
  Logger.info('[Core] Bootstrap application', config);
  LogConfig.useConsole();
  Application.initState();
  bootstrapElectronEvents();

  // Init net services
  NetService.bootstrap();
  const _end = performance.now();
  Application.isBootstrapped = true;
  Application.bootstrapTime = _end - _start;
  Logger.info(
    `[Core] Bootstrap application time: ${Application.bootstrapTime}ms`
  );
};

// Bootstrap application flow
