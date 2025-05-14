import { Route } from '@angular/router';

export const appRoutes: Route[] = [
  {
    path: '',
    loadComponent: () => import('./pages/project.component').then((m) => m.ProjectComponent),
  }
];
