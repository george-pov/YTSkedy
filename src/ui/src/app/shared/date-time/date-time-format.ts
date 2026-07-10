import { DateTime } from 'luxon';

export const DEFAULT_DATE_TIME_FORMAT = 'cccc, LLLL d, yyyy h:mm a';
export const DATE_INPUT_FORMAT = 'yyyy-MM-dd';
export const DATE_INPUT_DISPLAY_FORMAT = 'yyyy-MM-dd (cccc)';
export const TIME_INPUT_FORMAT = 'HH:mm';

export function formatUtcDateTime(epochMs: number): string {
  const instant = DateTime.fromMillis(epochMs, {
    locale: 'en-US',
    zone: 'utc',
  });

  return formatDateTime(instant);
}

export function formatLocalDateTime(localDateTime: string): string {
  const value = DateTime.fromISO(localDateTime, {
    locale: 'en-US',
    zone: 'utc',
  });

  return formatDateTime(value);
}

function formatDateTime(value: DateTime): string {
  return value.isValid ? value.toFormat(DEFAULT_DATE_TIME_FORMAT) : '';
}
