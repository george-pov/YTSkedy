import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import {
  calendarEventsUrl,
  publishCalendarEventUrl,
} from './calendar-events-endpoint';

export type CalendarEventStatus = 'Draft' | 'Published';

export interface CalendarEvent {
  calendarEventId: string;
  start: CalendarEventStart;
  descriptions: CalendarEventDescription[];
  status: CalendarEventStatus;
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

export interface CreateCalendarEventRequest {
  start: ScheduledStart;
  descriptions: LocalizedDescription[];
}

export interface ScheduledStart {
  localDateTime: string;
  timeZoneId: string;
}

export interface LocalizedDescription {
  language: string;
  title: string;
  description?: string;
}

export interface CreateCalendarEventResponse {
  calendarEventId: string;
}

export interface PublishCalendarEventResponse {
  calendarEventId: string;
  status: CalendarEventStatus;
  youTubeBroadcastId: string;
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

    return this.http.get<CalendarEvent[]>(calendarEventsUrl(this.appConfig.api), {
      params,
    });
  }

  create(
    request: CreateCalendarEventRequest,
  ): Observable<CreateCalendarEventResponse> {
    return this.http.post<CreateCalendarEventResponse>(
      calendarEventsUrl(this.appConfig.api),
      request,
    );
  }

  publish(
    calendarEventId: string,
  ): Observable<PublishCalendarEventResponse> {
    return this.http.post<PublishCalendarEventResponse>(
      publishCalendarEventUrl(this.appConfig.api, calendarEventId),
      {},
    );
  }
}
