import { ApiConfig } from 'src/app/shared/config/app-config';
import { normalizeApiBaseUrl } from 'src/app/shared/config/app-config-loader';

const path = 'api/settings/calendar-event-start-defaults';

export function calendarEventStartDefaultsUrl(api: ApiConfig): string {
  return new URL(path, normalizeApiBaseUrl(api.baseUrl)).toString();
}

export function isCalendarEventStartDefaultsUrl(candidate: string, api: ApiConfig): boolean {
  return candidate === calendarEventStartDefaultsUrl(api);
}
