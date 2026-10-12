import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import { App } from './app';
import { APP_CONFIG } from './core/config/app-config';

const META_URL = 'http://api.test/api/v1/meta';

async function renderShell() {
  await render(App, {
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([]),
      { provide: APP_CONFIG, useValue: { apiBaseUrl: 'http://api.test' } },
    ],
  });
  return TestBed.inject(HttpTestingController);
}

describe('App shell', () => {
  it('shows primary navigation and a skip link', async () => {
    await renderShell();

    expect(screen.getByRole('navigation', { name: 'Primary' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Runs' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Skip to content' })).toBeTruthy();
  });

  it('shows environment and version when the API answers', async () => {
    const http = await renderShell();

    http.expectOne(META_URL).flush({ service: 'orbit-control-plane', version: '1.2.3+abc123', environment: 'Development' });

    expect(await screen.findByText(/Development · v1\.2\.3$/)).toBeTruthy();
    http.verify();
  });

  it('shows API offline when the meta call fails', async () => {
    const http = await renderShell();

    http.expectOne(META_URL).flush(null, { status: 503, statusText: 'Service Unavailable' });

    expect(await screen.findByText('API offline')).toBeTruthy();
  });
});
