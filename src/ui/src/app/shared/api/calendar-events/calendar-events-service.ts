import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import {
  calendarEventByIdUrl,
  calendarEventsUrl,
  publishCalendarEventUrl,
} from './calendar-events-endpoint';

export type CalendarEventStatus = 'Draft' | 'Publishing' | 'Published';

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

export type CalendarEventSortField = 'scheduledStart' | 'status' | 'timeZone' | 'title';

export type CalendarEventSortDirection = 'asc' | 'desc';

/**
 * Server-side paged and sorted list query. `year` and `month` are optional and
 * must be supplied together; the page no longer scopes by month, so they are
 * currently unused by callers but kept for the backend reader contract.
 */
export interface CalendarEventListQuery {
  page: number;
  pageSize: number;
  sort: CalendarEventSortField;
  direction: CalendarEventSortDirection;
  year?: number;
  month?: number;
}

/** One page of the server-side sorted calendar event list. */
export interface CalendarEventListPage {
  items: CalendarEvent[];
  page: number;
  pageSize: number;
  totalCount: number;
  sort: CalendarEventSortField;
  direction: CalendarEventSortDirection;
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

export interface UpdateCalendarEventRequest {
  descriptions: LocalizedDescription[];
}

export interface UpdateCalendarEventResponse {
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

  list(query: CalendarEventListQuery): Observable<CalendarEventListPage> {
    let params = new HttpParams()
      .set('page', query.page.toString())
      .set('pageSize', query.pageSize.toString())
      .set('sort', query.sort)
      .set('direction', query.direction);

    if (query.year !== undefined && query.month !== undefined) {
      params = params.set('year', query.year.toString()).set('month', query.month.toString());
    }

    return this.http.get<CalendarEventListPage>(calendarEventsUrl(this.appConfig.api), { params });
  }

  getById(calendarEventId: string): Observable<CalendarEvent> {
    return this.http.get<CalendarEvent>(calendarEventByIdUrl(this.appConfig.api, calendarEventId));
  }

  update(
    calendarEventId: string,
    request: UpdateCalendarEventRequest,
  ): Observable<UpdateCalendarEventResponse> {
    return this.http.put<UpdateCalendarEventResponse>(
      calendarEventByIdUrl(this.appConfig.api, calendarEventId),
      request,
    );
  }

  create(request: CreateCalendarEventRequest): Observable<CreateCalendarEventResponse> {
    return this.http.post<CreateCalendarEventResponse>(
      calendarEventsUrl(this.appConfig.api),
      request,
    );
  }

  publish(calendarEventId: string): Observable<PublishCalendarEventResponse> {
    return this.http.post<PublishCalendarEventResponse>(
      publishCalendarEventUrl(this.appConfig.api, calendarEventId),
      {},
    );
  }
}
