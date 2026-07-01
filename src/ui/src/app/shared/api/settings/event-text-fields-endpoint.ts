import { ApiConfig } from 'src/app/shared/config/app-config';
import { normalizeApiBaseUrl } from 'src/app/shared/config/app-config-loader';

const eventTextFieldsPath = 'api/settings/event-text-fields';

export function eventTextFieldsUrl(api: ApiConfig): string {
  return new URL(eventTextFieldsPath, normalizeApiBaseUrl(api.baseUrl)).toString();
}

export function isEventTextFieldsUrl(candidate: string, api: ApiConfig): boolean {
  const prefix = eventTextFieldsUrl(api);

  if (!candidate.startsWith(prefix)) {
    return false;
  }

  const tail = candidate.substring(prefix.length);
  return tail.length === 0 || tail.startsWith('?');
}
