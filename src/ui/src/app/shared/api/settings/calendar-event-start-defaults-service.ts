import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { calendarEventStartDefaultsUrl } from './calendar-event-start-defaults-endpoint';

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

@Injectable({ providedIn: 'root' })
export class CalendarEventStartDefaultsService {
  private readonly http = inject(HttpClient);
  private readonly appConfig = inject(APP_CONFIG);

  get(): Observable<CalendarEventStartDefaultsResponse> {
    return this.http.get<CalendarEventStartDefaultsResponse>(
      calendarEventStartDefaultsUrl(this.appConfig.api),
    );
  }

  update(
    request: UpdateCalendarEventStartDefaultsRequest,
  ): Observable<CalendarEventStartDefaultsResponse> {
    return this.http.put<CalendarEventStartDefaultsResponse>(
      calendarEventStartDefaultsUrl(this.appConfig.api),
      request,
    );
  }
}
