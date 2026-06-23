import { ApiConfig } from 'src/app/shared/config/app-config';
import { normalizeApiBaseUrl } from 'src/app/shared/config/app-config-loader';

const platformsPath = 'api/platforms';

export function platformsUrl(api: ApiConfig): string {
  return new URL(platformsPath, normalizeApiBaseUrl(api.baseUrl)).toString();
}

export function platformByIdUrl(api: ApiConfig, id: string): string {
  const path = `${platformsPath}/${encodeURIComponent(id)}`;

  return new URL(path, normalizeApiBaseUrl(api.baseUrl)).toString();
}

export function isPlatformsUrl(candidate: string, api: ApiConfig): boolean {
  const prefix = platformsUrl(api);

  if (!candidate.startsWith(prefix)) {
    return false;
  }

  const tail = candidate.substring(prefix.length);
  return tail.length === 0 || tail.startsWith('?') || tail.startsWith('/');
}
