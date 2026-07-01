import { isCalendarEventsUrl } from 'src/app/shared/api/calendar-events/calendar-events-endpoint';
import { isPlatformsUrl } from 'src/app/shared/api/platforms/platforms-endpoint';
import { isEventTextFieldsUrl } from 'src/app/shared/api/settings/event-text-fields-endpoint';
import {
  isTemplatesUrl,
  isTemplateTokensUrl,
} from 'src/app/shared/api/templates/templates-endpoint';
import { ApiConfig, AuthConfig } from 'src/app/shared/config/app-config';

/**
 * Returns the scopes required to call the given URL, or `null` if the URL
 * is not a protected YTSkedy API resource.
 *
 * The `/api/calendar-events`, `/api/platforms`, `/api/templates`,
 * `/api/template-tokens`, and `/api/settings/event-text-fields` endpoints all
 * sit behind authentication. Platforms, templates, and settings reuse the
 * calendar event scopes; the read-vs-write distinction is enforced server-side
 * by `[RequiredScope]`, not by which scopes the SPA attaches, so both scopes
 * are requested for any protected call to prime MSAL's silent-token cache for
 * the session.
 */
export function getRequiredScopes(
  url: string,
  api: ApiConfig,
  auth: AuthConfig,
): string[] | null {
  if (
    isCalendarEventsUrl(url, api) ||
    isPlatformsUrl(url, api) ||
    isEventTextFieldsUrl(url, api) ||
    isTemplatesUrl(url, api) ||
    isTemplateTokensUrl(url, api)
  ) {
    return [auth.calendarEventsReadScope, auth.calendarEventsWriteScope];
  }

  return null;
}
