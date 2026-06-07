import { AppConfig, AuthConfig } from '../app-config';

export function testAuthConfig(overrides: Partial<AuthConfig> = {}): AuthConfig {
  return {
    clientId: 'spa-client-id',
    authority: 'https://tenant.ciamlogin.com/tenant-id/v2.0',
    knownAuthorities: ['tenant.ciamlogin.com'],
    redirectUri: 'http://localhost:4200',
    postLogoutRedirectUri: 'http://localhost:4200/signed-out',
    calendarEventsReadScope: 'api://ytskedy-api/CalendarEvents.Read',
    calendarEventsWriteScope: 'api://ytskedy-api/CalendarEvents.Write',
    ...overrides,
  };
}

export function testAppConfig(overrides: Partial<AppConfig> = {}): AppConfig {
  return {
    api: { baseUrl: 'https://api.example.test/' },
    auth: testAuthConfig(),
    ...overrides,
  };
}
