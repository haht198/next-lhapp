export interface ICFAppModule {
  bootstrap: (payload?: any) => void;
  bootstrapAsync?: (payload?: any) => Promise<void>;
}
