import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { normalizeApiBaseUrl } from 'src/app/shared/config/app-config-loader';

export interface CalendarEvent {
  calendarEventId: string;
  start: CalendarEventStart;
  descriptions: CalendarEventDescription[];
}

export interface CalendarEventStart {
  localDateTime: string;
  timeZoneId: string;
}

export interface CalendarEventDescription {
  language: string;
  title: string;
  description: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class CalendarEventsService {
  private readonly http = inject(HttpClient);
  private readonly appConfig = inject(APP_CONFIG);

  listByMonth(year: number, month: number): Observable<CalendarEvent[]> {
    const params = new HttpParams()
      .set('year', year.toString())
      .set('month', month.toString());

    return this.http.get<CalendarEvent[]>(this.calendarEventsUrl(), { params });
  }

  private calendarEventsUrl(): string {
    return new URL(
      'api/calendar-events',
      normalizeApiBaseUrl(this.appConfig.api.baseUrl),
    ).toString();
  }
}
