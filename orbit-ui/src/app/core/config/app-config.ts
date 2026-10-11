import { InjectionToken } from '@angular/core';

/** Runtime config, loaded from /config.json before bootstrap so one build serves every environment. */
export interface AppConfig {
  apiBaseUrl: string;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

export async function loadAppConfig(fetchFn: typeof fetch = fetch): Promise<AppConfig> {
  const response = await fetchFn('/config.json', { cache: 'no-store' });
  if (!response.ok) {
    throw new Error(`Failed to load /config.json (HTTP ${response.status})`);
  }
  const config = (await response.json()) as Partial<AppConfig>;
  if (typeof config.apiBaseUrl !== 'string' || config.apiBaseUrl === '') {
    throw new Error('config.json is missing apiBaseUrl');
  }
  return { apiBaseUrl: config.apiBaseUrl.replace(/\/+$/, '') };
}
