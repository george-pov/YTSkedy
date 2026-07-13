import { ApiConfig } from 'src/app/shared/config/app-config';
import { normalizeApiBaseUrl } from 'src/app/shared/config/app-config-loader';

const path = 'api/settings/calendar-event-defaults';

export function calendarEventDefaultsUrl(api: ApiConfig): string {
  return new URL(path, normalizeApiBaseUrl(api.baseUrl)).toString();
}

export function isCalendarEventDefaultsUrl(candidate: string, api: ApiConfig): boolean {
  const prefix = calendarEventDefaultsUrl(api);

  if (!candidate.startsWith(prefix)) {
    return false;
  }

  const tail = candidate.substring(prefix.length);
  return tail.length === 0 || tail.startsWith('?');
}
