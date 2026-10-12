import { httpResource } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { APP_CONFIG } from '../config/app-config';

/** Mirrors the control plane's MetaResponse (GET /api/v1/meta). */
export interface Meta {
  service: string;
  version: string;
  environment: string;
}

@Injectable({ providedIn: 'root' })
export class MetaApi {
  private readonly config = inject(APP_CONFIG);

  readonly meta = httpResource<Meta>(() => `${this.config.apiBaseUrl}/api/v1/meta`);
}
