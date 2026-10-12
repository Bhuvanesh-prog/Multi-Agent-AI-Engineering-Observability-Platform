import { bootstrapApplication } from '@angular/platform-browser';
import { App } from './app/app';
import { appConfigFor } from './app/app.config';
import { loadAppConfig } from './app/core/config/app-config';

loadAppConfig()
  .then((config) => bootstrapApplication(App, appConfigFor(config)))
  .catch((err: unknown) => {
    console.error(err);
    document.body.textContent = 'Orbit failed to start: configuration could not be loaded.';
  });
