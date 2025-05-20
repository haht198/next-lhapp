import { HttpClient, HttpHeaders } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { AuthService } from "./auth.service";

export const CF_SCREEND = {
  LUMA: 5900,
  HUE: 5300,
}

export interface ApiResponse<T> {
  data: T;
  metadata: {
    code: number;
    message: string;
    requestId: string;
  };
}

@Injectable({
  providedIn: 'root'
})
export class CFAPIService {

    authService = inject(AuthService);

    private _httpClient = inject(HttpClient);

    private _baseUrl = 'https://api.creativeforce-dev.io';

    getMyUserInfo() {
      return this._httpClient.get<ApiResponse<any>>(`${this._baseUrl}/contact/v2/user`, { headers: this.getRequestHeaders() });
    }


    private getRequestHeaders() {
        const headers = new HttpHeaders({
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.authService.token()?.access_token}`,
          'User-Agent': 'Creative Force HUE',
          'x-screen-id': CF_SCREEND.HUE.toString(),
        });
        return headers;
    }
}