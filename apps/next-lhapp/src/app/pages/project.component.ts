import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CFRendererIPCEvent } from '@creative-force/electron-core/preload';

interface ElectronCore {
  getApplicationState(): Promise<ApplicationState>;
  onApplicationStateChange(
    callback: (state: ApplicationState) => void
  ): CFRendererIPCEvent;
}

interface ApplicationState {
  isReady?: boolean;
  version?: string;
  platform?: string;
  bootstrapTime: number;
  [key: string]: any;
}

declare global {
  interface Window {
    electronCore: ElectronCore;
  }
}

@Component({
  selector: 'app-project',
  template: `
    <div class="project-page">
      <h1>Application State</h1>
      <pre>{{ applicationState() | json }}</pre>
    </div>
  `,
  styles: [
    `
      .project-page {
        padding: 20px;
        background-color: #f0f0f0;
        border-radius: 8px;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      }
      h1 {
        color: #333;
        font-size: 24px;
        margin-bottom: 10px;
      }
      p {
        color: #666;
        font-size: 16px;
        line-height: 1.5;
      }
    `,
  ],
  standalone: true,
  imports: [CommonModule],
})
export class ProjectComponent implements OnInit, OnDestroy {
  applicationState = signal<ApplicationState>({
    isReady: false,
    version: '',
    platform: '',
    bootstrapTime: 0,
  });

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
  }

  ngOnDestroy() {
    if (this._subscription) {
      this._subscription.unsubscribe();
    }
  }
}
