import { describe, expect, it } from 'vitest';

import { formatUtcDateTime } from './date-time-format';

describe('formatUtcDateTime', () => {
  it('uses the default long English date-time format', () => {
    expect(formatUtcDateTime(Date.parse('2009-06-15T10:15:00Z'))).toBe(
      'Monday, June 15, 2009 10:15 AM',
    );
  });

  it('formats the instant in UTC instead of its source offset', () => {
    expect(formatUtcDateTime(Date.parse('2009-06-15T23:15:00-07:00'))).toBe(
      'Tuesday, June 16, 2009 6:15 AM',
    );
  });

  it('returns an empty string for an invalid instant', () => {
    expect(formatUtcDateTime(Number.NaN)).toBe('');
  });
});
