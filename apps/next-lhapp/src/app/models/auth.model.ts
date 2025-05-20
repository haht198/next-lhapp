export interface AccountConfig {
    authorizationEndpoint: string;
    cfAccountUrl: string;
    revokeTokenEndpoint: string;
    tokenEndpoint: string;
}


export interface LoginViaBrowserResult {
    accountConfig: AccountConfig;
    callbackParams: {
        code: string;
        state: string;
    };
    codeVerifier: string;
    redirectUri: string;
    redirectCallbackUri: string;
}
