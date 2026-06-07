import { isCalendarEventsUrl } from 'src/app/shared/api/calendar-events/calendar-events-endpoint';
import { ApiConfig, AuthConfig } from 'src/app/shared/config/app-config';

/**
 * Returns the scopes required to call the given URL, or `null` if the URL
 * is not a protected YTSkedy API resource.
 *
 * Decision #14: both `/api/calendar-events` endpoints (GET and POST) sit
 * behind authentication. Both scopes are requested for any calendar
 * events call so MSAL primes its silent-token cache for the session;
 * the read-vs-write distinction is enforced server-side by
 * `[RequiredScope]`, not by which scopes the SPA chose to attach.
 */
export function getRequiredScopes(
  url: string,
  api: ApiConfig,
  auth: AuthConfig,
): string[] | null {
  if (isCalendarEventsUrl(url, api)) {
    return [auth.calendarEventsReadScope, auth.calendarEventsWriteScope];
  }

  return null;
}
