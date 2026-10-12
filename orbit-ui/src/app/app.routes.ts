import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'runs' },
  { path: 'runs', loadChildren: () => import('./features/runs/runs.routes').then((m) => m.RUNS_ROUTES) },
  { path: '**', redirectTo: 'runs' },
];
