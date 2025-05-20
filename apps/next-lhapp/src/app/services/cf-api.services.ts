import { HttpClient, HttpHeaders } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { AuthService } from "./auth.service";
import { ApiTaskStatusIdEnum } from "../constants/task";

export const CF_SCREEND = {
  LUMA: 5900,
  HUE: 5300,
}

export interface ApiResponse<T> {
  data: T;
  metadata: {
    code: string;
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


    getMyTasks() {
      const input = {
        statusIds: [ApiTaskStatusIdEnum.ToDo, ApiTaskStatusIdEnum.InProgress, ApiTaskStatusIdEnum.Rejected],
        pageSize: 100,
        pageNumber: 1
      }
      return this._httpClient.post<ApiResponse<any>>(`${this._baseUrl}/workflow/v2/internalpostproduction/getmytasks`, input, { headers: this.getRequestHeaders() });
    }

    getAllShootingType() {
      return this._httpClient.get<ApiResponse<any>>(`${this._baseUrl}/workflow/v2/shootingtypes/list?pageNumber=${1}&pageSize=${100}`, { headers: this.getRequestHeaders() });
    }

    getTaskDetail(taskIds: string[]) {
      return this._httpClient.post<ApiResponse<any>>(`${this._baseUrl}/workflow/v2/internalpostproduction/gettasksdetailv2`, taskIds, { headers: this.getRequestHeaders() });
    }


    private getRequestHeaders() {
        const headers = new HttpHeaders({
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.authService.token()?.access_token}`,
          'x-screen-id': CF_SCREEND.HUE.toString(),
        });
        return headers;
    }
}