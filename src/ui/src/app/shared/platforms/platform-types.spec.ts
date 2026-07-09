import { describe, expect, it } from 'vitest';

import { defaultPlatformType, isPlatformType, platformTypeOptions } from './platform-types';

describe('platform type metadata', () => {
  it('exposes the supported platform options in display order', () => {
    expect(platformTypeOptions).toEqual([
      { value: 'YouTube', label: 'YouTube' },
      { value: 'WordPress', label: 'WordPress' },
    ]);
  });

  it('keeps YouTube as the default create type', () => {
    expect(defaultPlatformType).toBe('YouTube');
  });

  it('identifies supported platform type values', () => {
    expect(isPlatformType('YouTube')).toBe(true);
    expect(isPlatformType('WordPress')).toBe(true);
    expect(isPlatformType('LinkedIn')).toBe(false);
  });
});
