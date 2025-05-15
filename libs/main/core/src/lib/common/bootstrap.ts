export interface ApplicationConfig {
    appName: string;
    appPort: number;
}

export const bootstrapApplicaion = (config: ApplicationConfig) => {
   console.log('[Core] Bootstrap application', config);
}