import { describe, expect, it } from 'vitest';

import { testAuthConfig } from 'src/app/shared/config/testing/app-config.fixture';
import { getRequiredScopes } from './protected-resources';

const auth = testAuthConfig();

const api = { baseUrl: 'https://api.example.test/' };

describe('resolveProtectedScopes', () => {
  it('returns both calendar events scopes for the exact list URL', () => {
    const scopes = getRequiredScopes(
      'https://api.example.test/api/calendar-events',
      api,
      auth,
    );

    expect(scopes).toEqual([
      auth.calendarEventsReadScope,
      auth.calendarEventsWriteScope,
    ]);
  });

  it('returns the scopes for a list call with query parameters', () => {
    const scopes = getRequiredScopes(
      'https://api.example.test/api/calendar-events?year=2026&month=6',
      api,
      auth,
    );

    expect(scopes).not.toBeNull();
  });

  it('returns the scopes for sub-paths under calendar-events', () => {
    const scopes = getRequiredScopes(
      'https://api.example.test/api/calendar-events/abc-123',
      api,
      auth,
    );

    expect(scopes).not.toBeNull();
  });

  it('returns the scopes for platforms API calls', () => {
    const scopes = getRequiredScopes(
      'https://api.example.test/api/platforms?type=YouTube',
      api,
      auth,
    );

    expect(scopes).toEqual([
      auth.calendarEventsReadScope,
      auth.calendarEventsWriteScope,
    ]);
  });

  it('returns the scopes for sub-paths under platforms', () => {
    const scopes = getRequiredScopes(
      'https://api.example.test/api/platforms/platform-1',
      api,
      auth,
    );

    expect(scopes).not.toBeNull();
  });

  it('returns the scopes for event text fields settings API calls', () => {
    const scopes = getRequiredScopes(
      'https://api.example.test/api/settings/event-text-fields',
      api,
      auth,
    );

    expect(scopes).toEqual([
      auth.calendarEventsReadScope,
      auth.calendarEventsWriteScope,
    ]);
  });

  it('returns the scopes for calendar event start defaults settings calls', () => {
    expect(
      getRequiredScopes(
        'https://api.example.test/api/settings/calendar-event-start-defaults',
        api,
        auth,
      ),
    ).toEqual([auth.calendarEventsReadScope, auth.calendarEventsWriteScope]);
  });

  it('does not match a path that merely starts with the event text fields prefix string', () => {
    const scopes = getRequiredScopes(
      'https://api.example.test/api/settings/event-text-fields-other',
      api,
      auth,
    );

    expect(scopes).toBeNull();
  });

  it('returns null for URLs outside the API base', () => {
    const scopes = getRequiredScopes(
      'https://other.example.test/api/calendar-events',
      api,
      auth,
    );

    expect(scopes).toBeNull();
  });

  it('returns null for non-calendar-events API paths', () => {
    const scopes = getRequiredScopes(
      'https://api.example.test/api/something-else',
      api,
      auth,
    );

    expect(scopes).toBeNull();
  });

  it('does not match a path that merely starts with the calendar-events prefix string', () => {
    const scopes = getRequiredScopes(
      'https://api.example.test/api/calendar-events-other',
      api,
      auth,
    );

    expect(scopes).toBeNull();
  });

  it('does not match a path that merely starts with the platforms prefix string', () => {
    const scopes = getRequiredScopes(
      'https://api.example.test/api/platforms-other',
      api,
      auth,
    );

    expect(scopes).toBeNull();
  });

  it('handles a base URL without a trailing slash', () => {
    const scopes = getRequiredScopes(
      'https://api.example.test/api/calendar-events',
      { baseUrl: 'https://api.example.test' },
      auth,
    );

    expect(scopes).not.toBeNull();
  });
});
