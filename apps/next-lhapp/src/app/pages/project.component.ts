import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CFRendererIPCEvent } from '@creative-force/electron-core/preload';
import { MatExpansionModule } from '@angular/material/expansion';
import { ApplicationState } from '../models/poroject.model';
import { AuthService } from '../services/auth.service';
import { HttpClient } from '@angular/common/http';
import { CFAPIService } from '../services/cf-api.services';
import { InternalPostComponent } from '../components/internal-post.component';


@Component({
  selector: 'app-project',
  templateUrl: './project.component.html',
  styleUrls: ['./project.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    MatExpansionModule,
    InternalPostComponent
  ]
})
export class ProjectComponent implements OnInit, OnDestroy {
  httpClient = inject(HttpClient);
  authService = inject(AuthService);
  cfApiService = inject(CFAPIService);
  applicationState = signal<ApplicationState>({
    isReady: false,
    version: '',
    platform: '',
    bootstrapTime: 0,
  });

  userInfo = signal<any>(null);

  readonly mockAPI = 'https://660ab815ccda4cbc75db9f5c.mockapi.io/user'
  private _subscription: CFRendererIPCEvent | null = null;

  ngOnInit() {
    window.electronCore
      .getApplicationState()
      .then((state: ApplicationState) => {
        console.log(state);
        this.applicationState.set(state);
      });

    this._subscription = window.electronCore.onApplicationStateChange(
      (newData: ApplicationState) => {
        console.log('onApplicationStateChange', newData);

        this.applicationState.update((state) => ({
          ...state,
          ...newData,
        }));
      }
    );
    this.authService.token$.subscribe((token) => {
      console.log('token', token);
      if (!token) {
        // remove user info
        this.userInfo.set(null);
        return;
      }
      this.getMyUserInfo();
    });
  }

  ngAfterViewInit() {
    this.authService.initialize();
  }

  ngOnDestroy() {
    if (this._subscription) {
      this._subscription.unsubscribe();
    }
  }

  getMyUserInfo() {
    this.cfApiService.getMyUserInfo().subscribe((res) => {
      if  (res.data) {
        this.userInfo.set(res.data);
      }
    });
  }
 
}
