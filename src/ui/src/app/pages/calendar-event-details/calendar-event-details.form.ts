import {
  AbstractControl,
  FormControl,
  FormGroup,
  ValidationErrors,
  Validators,
} from '@angular/forms';

import {
  CalendarEvent,
  CreateCalendarEventRequest,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { SelectOption } from 'src/app/shared/components/select/select';

export const titleMaxLength = 100;
export const descriptionMaxLength = 5000;

export type LanguageDescriptionGroup = FormGroup<{
  title: FormControl<string>;
  description: FormControl<string>;
}>;

export type CalendarEventDetailsForm = FormGroup<{
  start: FormGroup<{
    date: FormControl<string>;
    time: FormControl<string>;
    timeZoneId: FormControl<string>;
  }>;
  descriptions: FormGroup<{
    en: LanguageDescriptionGroup;
    ru: LanguageDescriptionGroup;
  }>;
}>;

// Curated short list of supported time zones. Not the full IANA set. The
// select stores and sends the chosen IANA id verbatim.
export const timeZoneOptions: readonly SelectOption[] = [
  { value: 'America/Vancouver', label: 'America/Vancouver' },
  { value: 'America/Los_Angeles', label: 'America/Los_Angeles' },
  { value: 'America/New_York', label: 'America/New_York' },
  { value: 'America/Chicago', label: 'America/Chicago' },
  { value: 'Europe/London', label: 'Europe/London' },
  { value: 'Europe/Moscow', label: 'Europe/Moscow' },
  { value: 'UTC', label: 'UTC' },
];

// Prefills the browser's detected zone only when it is in the curated list.
// Otherwise returns an empty value so the operator must choose.
export function detectPreselectedTimeZone(
  options: readonly SelectOption[] = timeZoneOptions,
  detectedTimeZone: string = Intl.DateTimeFormat().resolvedOptions().timeZone,
): string {
  return options.some((option) => option.value === detectedTimeZone) ? detectedTimeZone : '';
}

// Fails as `required` when the value is empty after trimming, so whitespace
// alone does not satisfy a required text field. Reuses the `required` key so
// pages map a single message.
export function requiredTrimmed(control: AbstractControl): ValidationErrors | null {
  const value = (control.value ?? '') as string;
  return value.trim().length === 0 ? { required: true } : null;
}

// Cross-field validator on the `start` group. Skips while any part is missing
// (the per-control required validators cover that). When all parts are
// present, resolves the wall-clock time in the selected zone to an instant and
// flags `startInPast` when it is not in the future. Client-side only, for
// responsiveness; the backend remains the durable validator.
export function futureStartValidator(group: AbstractControl): ValidationErrors | null {
  const date = group.get('date')?.value as string | undefined;
  const time = group.get('time')?.value as string | undefined;
  const timeZoneId = group.get('timeZoneId')?.value as string | undefined;

  if (!date || !time || !timeZoneId) {
    return null;
  }

  const startInstant = zonedWallTimeToInstant(`${date}T${time}:00`, timeZoneId);
  if (startInstant === null) {
    return null;
  }

  return startInstant > Date.now() ? null : { startInPast: true };
}

// Resolves a wall-clock local date-time in a named time zone to an epoch
// milliseconds instant. Uses the zone offset reported by Intl for that instant.
// DST transition edges are approximate; acceptable for client-side gating only.
function zonedWallTimeToInstant(localDateTime: string, timeZoneId: string): number | null {
  const naiveUtc = Date.parse(`${localDateTime}Z`);
  if (Number.isNaN(naiveUtc)) {
    return null;
  }

  const offsetMs = timeZoneOffsetMs(timeZoneId, naiveUtc);
  if (offsetMs === null) {
    return null;
  }

  return naiveUtc - offsetMs;
}

function timeZoneOffsetMs(timeZoneId: string, instant: number): number | null {
  try {
    const formatter = new Intl.DateTimeFormat('en-US', {
      timeZone: timeZoneId,
      hourCycle: 'h23',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });

    const lookup: Record<string, number> = {};
    for (const part of formatter.formatToParts(new Date(instant))) {
      if (part.type !== 'literal') {
        lookup[part.type] = Number(part.value);
      }
    }

    const asUtc = Date.UTC(
      lookup['year'],
      lookup['month'] - 1,
      lookup['day'],
      lookup['hour'],
      lookup['minute'],
      lookup['second'],
    );

    return asUtc - instant;
  } catch {
    return null;
  }
}

function createDescriptionGroup(): LanguageDescriptionGroup {
  return new FormGroup({
    title: new FormControl('', {
      nonNullable: true,
      validators: [requiredTrimmed, Validators.maxLength(titleMaxLength)],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [requiredTrimmed, Validators.maxLength(descriptionMaxLength)],
    }),
  });
}

export function createCalendarEventDetailsForm(
  preselectedTimeZone: string = detectPreselectedTimeZone(),
): CalendarEventDetailsForm {
  return new FormGroup({
    start: new FormGroup(
      {
        date: new FormControl('', {
          nonNullable: true,
          validators: [Validators.required],
        }),
        time: new FormControl('', {
          nonNullable: true,
          validators: [Validators.required],
        }),
        timeZoneId: new FormControl(preselectedTimeZone, {
          nonNullable: true,
          validators: [Validators.required],
        }),
      },
      { validators: [futureStartValidator] },
    ),
    descriptions: new FormGroup({
      en: createDescriptionGroup(),
      ru: createDescriptionGroup(),
    }),
  });
}

// Pure mapping from the form value to the create request. Combines the native
// date and time into `YYYY-MM-DDTHH:mm:ss` (explicit `:00` seconds), trims text
// values, and orders descriptions `en` then `ru`.
export function toCreateCalendarEventRequest(
  form: CalendarEventDetailsForm,
): CreateCalendarEventRequest {
  const value = form.getRawValue();

  return {
    start: {
      localDateTime: `${value.start.date}T${value.start.time}:00`,
      timeZoneId: value.start.timeZoneId,
    },
    descriptions: [
      {
        language: 'en',
        title: value.descriptions.en.title.trim(),
        description: value.descriptions.en.description.trim(),
      },
      {
        language: 'ru',
        title: value.descriptions.ru.title.trim(),
        description: value.descriptions.ru.description.trim(),
      },
    ],
  };
}

// Reverse mapping used by edit mode. Splits the stored wall-clock
// `YYYY-MM-DDTHH:mm:ss` into the native date (`YYYY-MM-DD`) and time (`HH:mm`)
// the controls hold, selects the stored time zone, and fills each language from
// its description (a missing language or null description becomes empty). Saving
// an edit is not wired yet; this only repopulates the form for display.
export function patchCalendarEventDetailsForm(
  form: CalendarEventDetailsForm,
  event: CalendarEvent,
): void {
  const match = /^(\d{4}-\d{2}-\d{2})T(\d{2}:\d{2})/.exec(event.start.localDateTime);
  const date = match === null ? '' : match[1];
  const time = match === null ? '' : match[2];

  const descriptionFor = (language: string) =>
    event.descriptions.find((entry) => entry.language === language);
  const en = descriptionFor('en');
  const ru = descriptionFor('ru');

  form.setValue({
    start: {
      date,
      time,
      timeZoneId: event.start.timeZoneId,
    },
    descriptions: {
      en: {
        title: en?.title ?? '',
        description: en?.description ?? '',
      },
      ru: {
        title: ru?.title ?? '',
        description: ru?.description ?? '',
      },
    },
  });
}
