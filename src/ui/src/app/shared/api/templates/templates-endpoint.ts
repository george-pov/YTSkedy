import { ApiConfig } from 'src/app/shared/config/app-config';
import { normalizeApiBaseUrl } from 'src/app/shared/config/app-config-loader';

const templatesPath = 'api/templates';
const templateTokensPath = 'api/template-tokens';

export function templatesUrl(api: ApiConfig): string {
  return new URL(templatesPath, normalizeApiBaseUrl(api.baseUrl)).toString();
}

export function templateByKeyUrl(api: ApiConfig, type: string, id: string): string {
  const path = `${templatesPath}/${encodeURIComponent(type)}/${encodeURIComponent(id)}`;

  return new URL(path, normalizeApiBaseUrl(api.baseUrl)).toString();
}

export function templateTokensUrl(api: ApiConfig): string {
  return new URL(templateTokensPath, normalizeApiBaseUrl(api.baseUrl)).toString();
}

export function isTemplatesUrl(candidate: string, api: ApiConfig): boolean {
  const prefix = templatesUrl(api);

  if (!candidate.startsWith(prefix)) {
    return false;
  }

  const tail = candidate.substring(prefix.length);
  return tail.length === 0 || tail.startsWith('?') || tail.startsWith('/');
}

export function isTemplateTokensUrl(candidate: string, api: ApiConfig): boolean {
  const prefix = templateTokensUrl(api);

  if (!candidate.startsWith(prefix)) {
    return false;
  }

  const tail = candidate.substring(prefix.length);
  return tail.length === 0 || tail.startsWith('?');
}
