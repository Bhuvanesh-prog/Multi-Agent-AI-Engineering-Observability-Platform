import { provideHttpClient, withFetch } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding, withViewTransitions } from '@angular/router';
import { routes } from './app.routes';
import { APP_CONFIG, AppConfig } from './core/config/app-config';

export function appConfigFor(config: AppConfig): ApplicationConfig {
  return {
    providers: [
      provideBrowserGlobalErrorListeners(),
      provideHttpClient(withFetch()),
      provideRouter(routes, withComponentInputBinding(), withViewTransitions()),
      { provide: APP_CONFIG, useValue: config },
    ],
  };
}
