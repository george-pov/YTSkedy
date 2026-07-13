import { describe, expect, it } from 'vitest';

import {
  createStartDefaultsModel,
  sameUpdateStartDefaultsRequest,
  startDefaultsTimeZoneOptions,
  toUpdateStartDefaultsRequest,
  weekdayOptions,
} from './start-defaults.form';

describe('start defaults form mapping', () => {
  it('maps full, partial, and empty responses to control values', () => {
    expect(
      createStartDefaultsModel({
        dayOfWeek: 'Friday',
        localTime: '09:05',
        timeZoneId: 'UTC',
      }),
    ).toEqual({ dayOfWeek: 'Friday', localTime: '09:05', timeZoneId: 'UTC' });
    expect(createStartDefaultsModel()).toEqual({ dayOfWeek: '', localTime: '', timeZoneId: '' });
  });

  it('normalizes cleared controls to null', () => {
    expect(
      toUpdateStartDefaultsRequest({ dayOfWeek: '', localTime: '', timeZoneId: '' }),
    ).toEqual({ dayOfWeek: null, localTime: null, timeZoneId: null });
  });

  it('provides canonical weekdays and the shared zones after No default', () => {
    expect(weekdayOptions.map((option) => option.value)).toEqual([
      '',
      'Sunday',
      'Monday',
      'Tuesday',
      'Wednesday',
      'Thursday',
      'Friday',
      'Saturday',
    ]);
    expect(startDefaultsTimeZoneOptions[0]).toEqual({ value: '', label: 'No default' });
    expect(startDefaultsTimeZoneOptions[1].value).toBe('America/Vancouver');
  });

  it('compares normalized requests', () => {
    const request = { dayOfWeek: null, localTime: '10:00', timeZoneId: null };
    expect(sameUpdateStartDefaultsRequest(request, { ...request })).toBe(true);
    expect(sameUpdateStartDefaultsRequest(request, { ...request, localTime: null })).toBe(false);
  });
});
