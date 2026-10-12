import { loadAppConfig } from './app-config';

function fakeFetch(status: number, body: unknown): typeof fetch {
  return () => Promise.resolve(new Response(JSON.stringify(body), { status }));
}

describe('loadAppConfig', () => {
  it('returns the config and trims trailing slashes', async () => {
    const config = await loadAppConfig(fakeFetch(200, { apiBaseUrl: 'https://api.orbit.dev//' }));

    expect(config.apiBaseUrl).toBe('https://api.orbit.dev');
  });

  it('rejects when config.json is missing', async () => {
    await expect(loadAppConfig(fakeFetch(404, {}))).rejects.toThrow('HTTP 404');
  });

  it('rejects when apiBaseUrl is absent', async () => {
    await expect(loadAppConfig(fakeFetch(200, {}))).rejects.toThrow('apiBaseUrl');
  });
});
