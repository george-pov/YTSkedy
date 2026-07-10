import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { EventTextType } from 'src/app/shared/api/settings/event-text-fields-service';
import {
  calendarEventByIdUrl,
  calendarEventsUrl,
  calendarEventThumbnailUrl,
  deletePlatformPublicationUrl,
  publishingContentUrl,
  publishPlatformUrl,
} from './calendar-events-endpoint';

export type CalendarEventPlatformStatus = 'NotPublished' | 'Publishing' | 'Published';
export type CalendarEventPublishingStatus =
  | 'NotPublished'
  | 'PartiallyPublished'
  | 'FullyPublished'
  | 'Failed';
export type ThumbnailPublishStatus = 'NotConfigured' | 'Applied' | 'Failed';
export type PublishingContentType = 'Preview' | 'Snapshot';

export interface CalendarEventFields {
  calendarEventId: string;
  start: CalendarEventStart;
  scheduledStartUtc: string;
  displayTitle: string;
  texts: CalendarEventText[];
}

export interface CalendarEvent extends CalendarEventFields {
  publicationStatus: CalendarEventPublishingStatus;
}

export interface CalendarEventDetailsResponse extends CalendarEventFields {
  canUpdate: boolean;
  canDelete: boolean;
  thumbnail: CalendarEventThumbnail | null;
  canUpdateThumbnail: boolean;
  platforms: CalendarEventPlatform[];
}

export interface CalendarEventThumbnail {
  fileName: string;
  contentType: string;
  sizeBytes: number;
  width: number;
  height: number;
  updatedUtc: string;
}

export interface CalendarEventPlatform {
  platformId: string;
  platformName: string;
  platformType: string;
  status: CalendarEventPlatformStatus;
  externalResourceId: string | null;
  thumbnailStatus: ThumbnailPublishStatus | null;
  publishedUtc: string | null;
  platformDeletedUtc: string | null;
  canPublish: boolean;
  canDeletePublication: boolean;
  canPreviewPublishingContent: boolean;
}

export interface EventPlatformPublishingContent {
  type: PublishingContentType;
  title: string;
  description: string | null;
}

export interface CalendarEventStart {
  localDateTime: string;
  timeZoneId: string;
}

export interface CalendarEventText {
  fieldKey: string;
  label: string;
  type: EventTextType;
  maxLength: number;
  value: string;
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
  texts: EventTextValue[];
}

export interface ScheduledStart {
  localDateTime: string;
  timeZoneId: string;
}

export interface EventTextValue {
  fieldKey: string;
  value: string;
}

export interface CreateCalendarEventResponse {
  calendarEventId: string;
}

export interface UpdateCalendarEventRequest {
  start: ScheduledStart;
  texts: EventTextValue[];
}

export interface UpdateCalendarEventResponse {
  calendarEventId: string;
}

export type PublishPlatformResponse = CalendarEventPlatform;

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

  getById(calendarEventId: string): Observable<CalendarEventDetailsResponse> {
    return this.http.get<CalendarEventDetailsResponse>(
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

  getPublishingContent(
    calendarEventId: string,
    platformId: string,
  ): Observable<EventPlatformPublishingContent> {
    return this.http.get<EventPlatformPublishingContent>(
      publishingContentUrl(this.appConfig.api, calendarEventId, platformId),
    );
  }

  delete(calendarEventId: string): Observable<void> {
    return this.http.delete<void>(calendarEventByIdUrl(this.appConfig.api, calendarEventId));
  }

  uploadThumbnail(
    calendarEventId: string,
    thumbnail: File,
  ): Observable<CalendarEventThumbnail> {
    const formData = new FormData();
    formData.append('thumbnail', thumbnail);

    return this.http.put<CalendarEventThumbnail>(
      calendarEventThumbnailUrl(this.appConfig.api, calendarEventId),
      formData,
    );
  }

  getThumbnail(calendarEventId: string): Observable<Blob> {
    return this.http.get(
      calendarEventThumbnailUrl(this.appConfig.api, calendarEventId),
      { responseType: 'blob' },
    );
  }

  deleteThumbnail(calendarEventId: string): Observable<void> {
    return this.http.delete<void>(
      calendarEventThumbnailUrl(this.appConfig.api, calendarEventId),
    );
  }
}
