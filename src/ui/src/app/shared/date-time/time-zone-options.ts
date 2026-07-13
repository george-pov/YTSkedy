import { SelectOption } from 'src/app/shared/components/select/select';

// Curated short list of supported time zones. Not the full IANA set.
export const timeZoneOptions: readonly SelectOption[] = [
  { value: 'America/Vancouver', label: 'America/Vancouver' },
  { value: 'America/Los_Angeles', label: 'America/Los_Angeles' },
  { value: 'America/New_York', label: 'America/New_York' },
  { value: 'America/Chicago', label: 'America/Chicago' },
  { value: 'Europe/London', label: 'Europe/London' },
  { value: 'Europe/Moscow', label: 'Europe/Moscow' },
  { value: 'UTC', label: 'UTC' },
];

export function detectPreselectedTimeZone(
  options: readonly SelectOption[] = timeZoneOptions,
  detectedTimeZone: string = Intl.DateTimeFormat().resolvedOptions().timeZone,
): string {
  return options.some((option) => option.value === detectedTimeZone) ? detectedTimeZone : '';
}
