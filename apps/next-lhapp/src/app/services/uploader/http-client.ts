import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { cloneDeep } from 'lodash';

export interface HTTPClientConfig {
    baseURL: string;
    token: string;
    appId: string;
    screenId: string;
}

export class HTTPClient {
    private _axios: AxiosInstance;
    private _config: HTTPClientConfig;
    
    constructor(config: HTTPClientConfig) {
        this._config = config;
        this._axios = axios.create({
            baseURL: config.baseURL,
            headers: {
                'X-App-Id': config.appId,
                'X-Screen-Id': config.screenId,
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${config.token}`,
            },
        });
        // this._axios.interceptors.request.use(this._handleRequest.bind(this));
        // this._axios.interceptors.response.use(this._handleResponse.bind(this));
    }

    async get(url: string, config?: AxiosRequestConfig) {
        return this._axios.get(url, config);
    }

    async post(url: string, data?: any, config?: AxiosRequestConfig) {
        return this._axios.post(url, data, config);
    }
    
    private _handleRequest(config: InternalAxiosRequestConfig) {
        console.log('[HTTPClient] Interceptor request', this._config);
        config = cloneDeep(config);
        config.headers['X-App-Id'] = this._config.appId;
        config.headers['X-Screen-Id'] = this._config.screenId;
        config.headers['Content-Type'] = 'application/json';
        config.headers['Authorization'] = `Bearer ${this._config.token}`;
        return config;
    }

    private _handleResponse(response: AxiosResponse) {
        console.log('[HTTPClient] Interceptor response', response);
        return response.data;
    }
    
    
}