import { WritableSignal } from '@angular/core';
import {
  disabled,
  maxLength,
  required,
  validate,
  validateTree,
  type SchemaPathTree,
} from '@angular/forms/signals';

import {
  CalendarEvent,
  CreateCalendarEventRequest,
  UpdateCalendarEventRequest,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { SelectOption } from 'src/app/shared/components/select/select';

export const titleMaxLength = 100;
export const descriptionMaxLength = 5000;

export interface LanguageDescription {
  title: string;
  description: string;
}

export interface CalendarEventDetailsModel {
  start: {
    date: string;
    time: string;
    timeZoneId: string;
  };
  descriptions: {
    en: LanguageDescription;
    ru: LanguageDescription;
  };
}

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

export function createCalendarEventDetailsModel(
  preselectedTimeZone: string = detectPreselectedTimeZone(),
): CalendarEventDetailsModel {
  return {
    start: { date: '', time: '', timeZoneId: preselectedTimeZone },
    descriptions: {
      en: { title: '', description: '' },
      ru: { title: '', description: '' },
    },
  };
}

// Signal Forms validation rules. Defined as a function so the page can close
// over its edit-mode signal: in edit mode the `start` group is disabled, which
// excludes it from validation (descriptions-only edit).
export function applyCalendarEventDetailsRules(
  path: SchemaPathTree<CalendarEventDetailsModel>,
  isEditMode: () => boolean,
): void {
  disabled(path.start, { when: () => isEditMode() });
  required(path.start.date, { message: 'Start date is required.' });
  required(path.start.time, { message: 'Start time is required.' });
  required(path.start.timeZoneId, { message: 'Time zone is required.' });

  // Cross-field on the `start` group: resolve the wall-clock start in the
  // chosen zone to an instant and flag a non-future start. Skips while any part
  // is missing (the per-control required rules cover that). Client-side only,
  // for responsiveness; the backend remains the durable validator.
  validateTree(path.start, ({ value }) => {
    const { date, time, timeZoneId } = value();
    if (!date || !time || !timeZoneId) {
      return undefined;
    }

    const startInstant = zonedWallTimeToInstant(`${date}T${time}:00`, timeZoneId);
    if (startInstant === null || startInstant > Date.now()) {
      return undefined;
    }

    return { kind: 'startInPast', message: 'Start must be in the future.' };
  });

  // Required-trimmed (reject whitespace-only) reuses the `required` error kind.
  validate(path.descriptions.en.title, ({ value }) =>
    value().trim().length === 0
      ? { kind: 'required', message: 'English title is required.' }
      : undefined,
  );
  maxLength(path.descriptions.en.title, titleMaxLength, {
    message: 'English title is too long.',
  });
  validate(path.descriptions.en.description, ({ value }) =>
    value().trim().length === 0
      ? { kind: 'required', message: 'English description is required.' }
      : undefined,
  );
  maxLength(path.descriptions.en.description, descriptionMaxLength, {
    message: 'English description is too long.',
  });

  validate(path.descriptions.ru.title, ({ value }) =>
    value().trim().length === 0
      ? { kind: 'required', message: 'Russian title is required.' }
      : undefined,
  );
  maxLength(path.descriptions.ru.title, titleMaxLength, {
    message: 'Russian title is too long.',
  });
  validate(path.descriptions.ru.description, ({ value }) =>
    value().trim().length === 0
      ? { kind: 'required', message: 'Russian description is required.' }
      : undefined,
  );
  maxLength(path.descriptions.ru.description, descriptionMaxLength, {
    message: 'Russian description is too long.',
  });
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

// Pure mapping from the model to the create request. Combines the native date
// and time into `YYYY-MM-DDTHH:mm:ss` (explicit `:00` seconds), trims text
// values, and orders descriptions `en` then `ru`.
export function toCreateCalendarEventRequest(
  model: CalendarEventDetailsModel,
): CreateCalendarEventRequest {
  return {
    start: {
      localDateTime: `${model.start.date}T${model.start.time}:00`,
      timeZoneId: model.start.timeZoneId,
    },
    descriptions: [
      {
        language: 'en',
        title: model.descriptions.en.title.trim(),
        description: model.descriptions.en.description.trim(),
      },
      {
        language: 'ru',
        title: model.descriptions.ru.title.trim(),
        description: model.descriptions.ru.description.trim(),
      },
    ],
  };
}

// Reverse mapping used by edit mode. Splits the stored wall-clock
// `YYYY-MM-DDTHH:mm:ss` into the native date (`YYYY-MM-DD`) and time (`HH:mm`)
// the model holds, selects the stored time zone, and fills each language from
// its description (a missing language or null description becomes empty).
export function patchCalendarEventDetailsModel(
  model: WritableSignal<CalendarEventDetailsModel>,
  event: CalendarEvent,
): void {
  const match = /^(\d{4}-\d{2}-\d{2})T(\d{2}:\d{2})/.exec(event.start.localDateTime);
  const date = match === null ? '' : match[1];
  const time = match === null ? '' : match[2];

  const descriptionFor = (language: string) =>
    event.descriptions.find((entry) => entry.language === language);
  const en = descriptionFor('en');
  const ru = descriptionFor('ru');

  model.set({
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

// Pure mapping from the model to the update request. Edit changes only the
// descriptions (the start is immutable), so it carries no start. Trims text and
// orders descriptions `en` then `ru`, mirroring the create request.
export function toUpdateCalendarEventRequest(
  model: CalendarEventDetailsModel,
): UpdateCalendarEventRequest {
  return {
    descriptions: [
      {
        language: 'en',
        title: model.descriptions.en.title.trim(),
        description: model.descriptions.en.description.trim(),
      },
      {
        language: 'ru',
        title: model.descriptions.ru.title.trim(),
        description: model.descriptions.ru.description.trim(),
      },
    ],
  };
}

// Formats an epoch-milliseconds instant as `YYYY-MM-DD HH:mm` in UTC, matching
// the calendar events list rendering of the scheduled start.
function formatUtcInstant(epochMs: number): string {
  const instant = new Date(epochMs);
  const pad = (value: number): string => value.toString().padStart(2, '0');

  return (
    `${instant.getUTCFullYear()}-${pad(instant.getUTCMonth() + 1)}-` +
    `${pad(instant.getUTCDate())} ${pad(instant.getUTCHours())}:` +
    `${pad(instant.getUTCMinutes())}`
  );
}

// Create-mode preview: resolves the chosen local date, time, and zone to the UTC
// instant the backend would store, so the form can show how the local start
// translates to UTC. Returns an empty string while any part is missing or the
// zone cannot be interpreted. Parts are optional because a typed form group's
// value omits disabled controls. Client-side conversion is approximate at DST
// edges; it is informational and the backend remains authoritative.
export function scheduledStartUtcPreview(
  date: string | undefined,
  time: string | undefined,
  timeZoneId: string | undefined,
): string {
  if (!date || !time || !timeZoneId) {
    return '';
  }

  const instant = zonedWallTimeToInstant(`${date}T${time}:00`, timeZoneId);

  return instant === null ? '' : formatUtcInstant(instant);
}

// Edit-mode: formats the stored UTC instant (an ISO-8601 string from the API)
// for the read-only display. Returns an empty string when it cannot be parsed.
export function formatScheduledStartUtcIso(scheduledStartUtc: string): string {
  const epochMs = Date.parse(scheduledStartUtc);

  return Number.isNaN(epochMs) ? '' : formatUtcInstant(epochMs);
}
