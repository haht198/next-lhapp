import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Injectable, inject, signal } from "@angular/core";
import { LoginViaBrowserResult } from "../models/auth.model";
import { firstValueFrom } from 'rxjs';
import { toObservable } from '@angular/core/rxjs-interop';
import qs from 'qs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {  
    private _httpClient = inject(HttpClient);
    clientId: 'hue' | 'luma' | 'ink' = 'luma';
 
    isInitialized = signal(false);

    token = signal<any | null>(null);
    token$ = toObservable(this.token);

    private _loginResult: LoginViaBrowserResult | null = null;
    private TOKEN_KEY = 'token';
    
    async initialize() {
        const result = await window.electron.auth.bootstrap(this.clientId);
        console.log('AuthService - initialized', result);
        this.isInitialized.set(result);
        this.checkToken();
    }

    checkToken() {
        const token = localStorage.getItem(this.TOKEN_KEY);
        if (token) {
            this.token.set(JSON.parse(token));
        }
    }   

    async login() {
        const result = await window.electron.auth.login() as LoginViaBrowserResult;
        console.log('AuthService - login', result);
        if (!result || !result.callbackParams) {
            console.error('AuthService - login error', result);
            return;
        }
        this._loginResult = result;
        await this.getTokenApi();
    }

    logout() {
        this.token.set(null);
        this._loginResult = null;
        localStorage.removeItem(this.TOKEN_KEY);
    }

    private async getTokenApi()  {
        try {
            if (!this._loginResult) {   
                throw new Error('Login result not found');
            }
            const body = qs.stringify({
                client_id: this.clientId,
                grant_type: 'authorization_code',
                redirect_uri: this._loginResult.redirectUri,
                code: this._loginResult.callbackParams.code,
                code_verifier: this._loginResult.codeVerifier,
            });
            console.log('AuthService - getTokenApi', body);
            const result = await firstValueFrom(
                this._httpClient.post<any>(
                    `${this._loginResult.accountConfig.tokenEndpoint}`,
                    body,
                    { headers: this.getRequestHeaders() }
                )
            );
            console.log('AuthService - getToken', result);
            this.token.set(result);
            localStorage.setItem(this.TOKEN_KEY, JSON.stringify(result));
        } catch (error) {
            console.error('AuthService - getToken error', error);
        }
    }

    private getRequestHeaders() {
        const headers = new HttpHeaders({
          'Content-Type': 'application/x-www-form-urlencoded',
        });
        return headers;
      }
}
