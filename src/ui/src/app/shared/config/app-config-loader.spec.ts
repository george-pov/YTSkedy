import { describe, expect, it } from 'vitest';

import { testAuthConfig } from './testing/app-config.fixture';
import {
  normalizeApiBaseUrl,
  parseAppConfig,
} from './app-config-loader';

const validAuth = {
  ...testAuthConfig({ postLogoutRedirectUri: 'http://localhost:4200' }),
};

const validApi = { baseUrl: 'https://api.example.test' };

describe('parseAppConfig', () => {
  it('requires api baseUrl to be configured', () => {
    expect(() =>
      parseAppConfig({ api: { baseUrl: '' }, auth: validAuth }),
    ).toThrow();
  });

  it('normalizes api baseUrl with a trailing slash', () => {
    const config = parseAppConfig({
      api: {
        baseUrl: 'https://api.example.test',
      },
      auth: validAuth,
    });

    expect(config.api.baseUrl).toBe('https://api.example.test/');
  });

  it('parses a valid auth section', () => {
    const config = parseAppConfig({ api: validApi, auth: validAuth });

    expect(config.auth).toEqual(validAuth);
  });

  it('trims surrounding whitespace from auth string fields', () => {
    const config = parseAppConfig({
      api: validApi,
      auth: {
        ...validAuth,
        clientId: '  spa-client-id  ',
        knownAuthorities: ['  tenant.ciamlogin.com  '],
      },
    });

    expect(config.auth.clientId).toBe('spa-client-id');
    expect(config.auth.knownAuthorities).toEqual(['tenant.ciamlogin.com']);
  });

  it('rejects a non-object auth section', () => {
    expect(() =>
      parseAppConfig({ api: validApi, auth: 'not-an-object' }),
    ).toThrow();
  });

  it('rejects a missing auth section', () => {
    expect(() => parseAppConfig({ api: validApi })).toThrow();
  });

  it.each([
    'clientId',
    'authority',
    'redirectUri',
    'postLogoutRedirectUri',
    'calendarEventsReadScope',
    'calendarEventsWriteScope',
  ] as const)('rejects a missing auth.%s field', (field) => {
    const auth = { ...validAuth } as Record<string, unknown>;
    delete auth[field];

    expect(() => parseAppConfig({ api: validApi, auth })).toThrow();
  });

  it.each([
    'clientId',
    'authority',
    'redirectUri',
    'postLogoutRedirectUri',
    'calendarEventsReadScope',
    'calendarEventsWriteScope',
  ] as const)('rejects an empty auth.%s field', (field) => {
    expect(() =>
      parseAppConfig({
        api: validApi,
        auth: { ...validAuth, [field]: '   ' },
      }),
    ).toThrow();
  });

  it('rejects a non-string auth.clientId', () => {
    expect(() =>
      parseAppConfig({
        api: validApi,
        auth: { ...validAuth, clientId: 42 },
      }),
    ).toThrow();
  });

  it('rejects a missing auth.knownAuthorities array', () => {
    const auth = { ...validAuth } as Record<string, unknown>;
    delete auth['knownAuthorities'];

    expect(() => parseAppConfig({ api: validApi, auth })).toThrow();
  });

  it('rejects a non-array auth.knownAuthorities', () => {
    expect(() =>
      parseAppConfig({
        api: validApi,
        auth: { ...validAuth, knownAuthorities: 'tenant.ciamlogin.com' },
      }),
    ).toThrow();
  });

  it('rejects an empty auth.knownAuthorities array', () => {
    expect(() =>
      parseAppConfig({
        api: validApi,
        auth: { ...validAuth, knownAuthorities: [] },
      }),
    ).toThrow();
  });

  it('rejects an auth.knownAuthorities entry that is not a string', () => {
    expect(() =>
      parseAppConfig({
        api: validApi,
        auth: { ...validAuth, knownAuthorities: ['tenant.ciamlogin.com', 7] },
      }),
    ).toThrow();
  });

  it('rejects an auth.knownAuthorities entry that is an empty string', () => {
    expect(() =>
      parseAppConfig({
        api: validApi,
        auth: { ...validAuth, knownAuthorities: ['  '] },
      }),
    ).toThrow();
  });
});

describe('normalizeApiBaseUrl', () => {
  it('preserves an existing trailing slash', () => {
    expect(normalizeApiBaseUrl('https://api.example.test/')).toBe(
      'https://api.example.test/',
    );
  });
});
