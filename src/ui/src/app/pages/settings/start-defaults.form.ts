import { SchemaPathTree } from '@angular/forms/signals';

import {
  CalendarEventStartDefaultsResponse,
  CalendarEventWeekday,
  UpdateCalendarEventStartDefaultsRequest,
} from 'src/app/shared/api/settings/calendar-event-defaults-service';
import { SelectOption } from 'src/app/shared/components/select/select';
import { timeZoneOptions } from 'src/app/shared/date-time/time-zone-options';
import { sameRequest } from 'src/app/shared/forms/request-comparison';

export interface StartDefaultsModel {
  dayOfWeek: CalendarEventWeekday | '';
  localTime: string;
  timeZoneId: string;
}

export const weekdayOptions: readonly SelectOption[] = [
  { value: '', label: 'No default' },
  ...(['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'] as const).map(
    (value) => ({ value, label: value }),
  ),
];

export const startDefaultsTimeZoneOptions: readonly SelectOption[] = [
  { value: '', label: 'No default' },
  ...timeZoneOptions,
];

export function createStartDefaultsModel(
  response?: CalendarEventStartDefaultsResponse,
): StartDefaultsModel {
  return {
    dayOfWeek: response?.dayOfWeek ?? '',
    localTime: response?.localTime ?? '',
    timeZoneId: response?.timeZoneId ?? '',
  };
}

export function applyStartDefaultsRules(_path: SchemaPathTree<StartDefaultsModel>): void {}

export function toUpdateStartDefaultsRequest(
  model: StartDefaultsModel,
): UpdateCalendarEventStartDefaultsRequest {
  return {
    dayOfWeek: model.dayOfWeek || null,
    localTime: model.localTime || null,
    timeZoneId: model.timeZoneId || null,
  };
}

export function sameUpdateStartDefaultsRequest(
  left: UpdateCalendarEventStartDefaultsRequest,
  right: UpdateCalendarEventStartDefaultsRequest,
): boolean {
  return sameRequest(left, right);
}
