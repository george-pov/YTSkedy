import { describe, expect, it } from 'vitest';

import {
  normalizeApiBaseUrl,
  parseAppConfig,
} from './app-config-loader';

describe('parseAppConfig', () => {
  it('requires api baseUrl to be configured', () => {
    expect(() => parseAppConfig({ api: { baseUrl: '' } })).toThrow(
      'Runtime config api.baseUrl must be a non-empty string.',
    );
  });

  it('normalizes api baseUrl with a trailing slash', () => {
    const config = parseAppConfig({
      api: {
        baseUrl: 'https://api.example.test',
      },
    });

    expect(config.api.baseUrl).toBe('https://api.example.test/');
  });
});

describe('normalizeApiBaseUrl', () => {
  it('preserves an existing trailing slash', () => {
    expect(normalizeApiBaseUrl('https://api.example.test/')).toBe(
      'https://api.example.test/',
    );
  });
});
