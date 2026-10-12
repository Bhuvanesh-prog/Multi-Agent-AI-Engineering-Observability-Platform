import { Routes } from '@angular/router';

export const RUNS_ROUTES: Routes = [
  {
    path: '',
    title: 'Runs · Orbit',
    loadComponent: () => import('./pages/runs-page').then((m) => m.RunsPage),
  },
];
