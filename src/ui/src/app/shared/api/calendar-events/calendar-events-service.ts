import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import {
  calendarEventByIdUrl,
  calendarEventsUrl,
  deletePlatformPublicationUrl,
  publishPlatformUrl,
} from './calendar-events-endpoint';

export type CalendarEventPlatformStatus = 'NotPublished' | 'Publishing' | 'Published';

interface CalendarEventFields {
  calendarEventId: string;
  start: CalendarEventStart;
  scheduledStartUtc: string;
  descriptions: CalendarEventDescription[];
}

export interface CalendarEvent extends CalendarEventFields {}

export interface CalendarEventDetail extends CalendarEventFields {
  platforms: CalendarEventPlatform[];
}

export interface CalendarEventPlatform {
  platformId: string;
  platformName: string;
  platformType: string;
  status: CalendarEventPlatformStatus;
  externalResourceId: string | null;
  publishedUtc: string | null;
  platformDeletedUtc: string | null;
  canPublish: boolean;
  canDeletePublication: boolean;
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

export type CalendarEventSortField = 'scheduledStart' | 'timeZone' | 'title';

export type CalendarEventSortDirection = 'asc' | 'desc';

/**
 * Server-side paged and sorted list query. `year` and `month` optionally scope
 * the backend reader when supplied together; list callers can omit them for an
 * all-events page.
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

export interface PublishPlatformResponse {
  calendarEventId: string;
  platformId: string;
  platformName: string;
  platformType: string;
  status: CalendarEventPlatformStatus;
  externalResourceId: string;
  publishedUtc: string;
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

  getById(calendarEventId: string): Observable<CalendarEventDetail> {
    return this.http.get<CalendarEventDetail>(
      calendarEventByIdUrl(this.appConfig.api, calendarEventId),
    );
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

  publishPlatform(
    calendarEventId: string,
    platformId: string,
  ): Observable<PublishPlatformResponse> {
    return this.http.post<PublishPlatformResponse>(
      publishPlatformUrl(this.appConfig.api, calendarEventId, platformId),
      {},
    );
  }

  deletePlatformPublication(
    calendarEventId: string,
    platformId: string,
  ): Observable<CalendarEventPlatform> {
    return this.http.delete<CalendarEventPlatform>(
      deletePlatformPublicationUrl(this.appConfig.api, calendarEventId, platformId),
    );
  }

  delete(calendarEventId: string): Observable<void> {
    return this.http.delete<void>(calendarEventByIdUrl(this.appConfig.api, calendarEventId));
  }
}
