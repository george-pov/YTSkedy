import { ApiConfig } from 'src/app/shared/config/app-config';
import { normalizeApiBaseUrl } from 'src/app/shared/config/app-config-loader';

const calendarEventsPath = 'api/calendar-events';

export function calendarEventsUrl(api: ApiConfig): string {
  return new URL(calendarEventsPath, normalizeApiBaseUrl(api.baseUrl)).toString();
}

export function publishCalendarEventUrl(
  api: ApiConfig,
  calendarEventId: string,
): string {
  const path = `${calendarEventsPath}/${encodeURIComponent(calendarEventId)}/publish`;

  return new URL(path, normalizeApiBaseUrl(api.baseUrl)).toString();
}

export function isCalendarEventsUrl(candidate: string, api: ApiConfig): boolean {
  const prefix = calendarEventsUrl(api);

  if (!candidate.startsWith(prefix)) {
    return false;
  }

  const tail = candidate.substring(prefix.length);
  return tail.length === 0 || tail.startsWith('?') || tail.startsWith('/');
}
