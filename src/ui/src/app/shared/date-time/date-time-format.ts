import { DateTime } from 'luxon';

export const DEFAULT_DATE_TIME_FORMAT = 'cccc, LLLL d, yyyy h:mm a';

export function formatUtcDateTime(epochMs: number): string {
  const instant = DateTime.fromMillis(epochMs, {
    locale: 'en-US',
    zone: 'utc',
  });

  return instant.isValid ? instant.toFormat(DEFAULT_DATE_TIME_FORMAT) : '';
}
