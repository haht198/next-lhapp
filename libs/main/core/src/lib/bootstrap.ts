import { NetService } from './net-service';
import { Application } from './static';
import Logger, { LogConfig } from '@creative-force/eslogger';
import { bootstrapElectronEvents } from './events/electron.event';

export interface ICFAppModule {
  bootstrap: () => void;
  bootstrapAsync?: () => Promise<void>;
}

export interface CFAppModuleProvider extends ICFAppModule {
  boostrapFactory: () => void;
}

export interface ApplicationConfig {
  appName: string;
  modules?: ICFAppModule[] | CFAppModuleProvider[];
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
  config.modules?.forEach((module) => {
    module?.bootstrap();
  });
};

// Bootstrap application flow
