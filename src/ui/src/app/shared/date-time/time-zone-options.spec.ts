import { describe, expect, it } from 'vitest';

import { detectPreselectedTimeZone, timeZoneOptions } from './time-zone-options';

describe('time-zone options', () => {
  it('preserves the curated IANA values', () => {
    expect(timeZoneOptions.map((option) => option.value)).toEqual([
      'America/Vancouver',
      'America/Los_Angeles',
      'America/New_York',
      'America/Chicago',
      'Europe/London',
      'Europe/Moscow',
      'UTC',
    ]);
  });

  it('selects only a supported browser zone', () => {
    expect(detectPreselectedTimeZone(timeZoneOptions, 'America/Vancouver')).toBe(
      'America/Vancouver',
    );
    expect(detectPreselectedTimeZone(timeZoneOptions, 'Asia/Tokyo')).toBe('');
  });
});
