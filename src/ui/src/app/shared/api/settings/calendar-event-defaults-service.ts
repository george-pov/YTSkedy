import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { calendarEventDefaultsUrl } from './calendar-event-defaults-endpoint';

export type EventTextType = 'ShortText' | 'LongText';

export interface EventTextField {
  fieldKey: string;
  label: string;
  type: EventTextType;
  maxLength: number;
}

export interface EventTextFieldsResponse {
  fields: EventTextField[];
}

export interface UpdateEventTextFieldsRequest {
  fields: EventTextField[];
}

export type CalendarEventWeekday =
  | 'Sunday'
  | 'Monday'
  | 'Tuesday'
  | 'Wednesday'
  | 'Thursday'
  | 'Friday'
  | 'Saturday';

export interface CalendarEventStartDefaultsResponse {
  dayOfWeek: CalendarEventWeekday | null;
  localTime: string | null;
  timeZoneId: string | null;
}

export type UpdateCalendarEventStartDefaultsRequest = CalendarEventStartDefaultsResponse;

export interface CalendarEventDefaultsResponse {
  eventTextFields: EventTextFieldsResponse;
  startDefaults: CalendarEventStartDefaultsResponse;
}

export type UpdateCalendarEventDefaultsRequest = CalendarEventDefaultsResponse;

@Injectable({ providedIn: 'root' })
export class CalendarEventDefaultsService {
  private readonly http = inject(HttpClient);
  private readonly appConfig = inject(APP_CONFIG);

  get(): Observable<CalendarEventDefaultsResponse> {
    return this.http.get<CalendarEventDefaultsResponse>(
      calendarEventDefaultsUrl(this.appConfig.api),
    );
  }

  update(request: UpdateCalendarEventDefaultsRequest): Observable<CalendarEventDefaultsResponse> {
    return this.http.put<CalendarEventDefaultsResponse>(
      calendarEventDefaultsUrl(this.appConfig.api),
      request,
    );
  }
}
