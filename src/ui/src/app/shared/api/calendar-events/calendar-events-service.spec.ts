import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { testAppConfig } from 'src/app/shared/config/testing/app-config.fixture';
import {
  CalendarEvent,
  CalendarEventDetailsResponse,
  CalendarEventListPage,
  CalendarEventPlatform,
  CalendarEventThumbnail,
  CalendarEventsService,
  CreateCalendarEventRequest,
  CreateCalendarEventResponse,
  EventPlatformPublishingContent,
  PublishPlatformResponse,
  UpdateCalendarEventRequest,
  UpdateCalendarEventResponse,
} from './calendar-events-service';

describe('CalendarEventsService', () => {
  let http: HttpTestingController;
  let service: CalendarEventsService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: APP_CONFIG,
          useValue: testAppConfig({
            api: { baseUrl: 'https://api.example.test' },
          }),
        },
      ],
    });

    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(CalendarEventsService);
  });

  afterEach(() => {
    http.verify();
  });

  it('gets the default start with an optional encoded fallback time zone', () => {
    const response = {
      localDate: '2026-07-12',
      localTime: '10:00',
      timeZoneId: 'America/Vancouver',
    };
    let actual;

    service.getDefaultStart('America/Vancouver').subscribe((value) => (actual = value));

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events/start-suggestion?fallbackTimeZoneId=America/Vancouver',
    );
    expect(request.request.method).toBe('GET');
    request.flush(response);
    expect(actual).toEqual(response);
  });

  it('omits the fallback query when no supported zone is available', () => {
    service.getDefaultStart().subscribe();

    const request = http.expectOne('https://api.example.test/api/calendar-events/start-suggestion');
    expect(request.request.params.keys()).toEqual([]);
    request.flush({ localDate: null, localTime: null, timeZoneId: null });
  });

  it('requests a calendar events page with the paging and sorting query and returns the envelope', () => {
    const apiResponse: CalendarEventListPage = {
      items: [
        {
          calendarEventId: '6f9619ff8b864fb5bdfd4f5c2f2f16a1',
          start: {
            localDateTime: '2026-01-15T09:30:00',
            timeZoneId: 'Etc/UTC',
          },
          scheduledStartUtc: '2026-01-15T09:30:00+00:00',
          displayTitle: 'Test stream',
          publicationStatus: 'PartiallyPublished',
          texts: [
            {
              fieldKey: 'text1',
              label: 'Title',
              type: 'ShortText',
              maxLength: 50,
              value: 'Test stream',
            },
          ],
        },
      ],
      page: 0,
      pageSize: 10,
      totalCount: 1,
      sort: 'scheduledStart',
      direction: 'desc',
    };

    let actualPage: CalendarEventListPage | undefined;
    service
      .list({ page: 0, pageSize: 10, sort: 'scheduledStart', direction: 'desc' })
      .subscribe((page) => {
        actualPage = page;
      });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events?page=0&pageSize=10&sort=scheduledStart&direction=desc',
    );

    expect(request.request.method).toBe('GET');

    request.flush(apiResponse);

    expect(actualPage).toEqual(apiResponse);
    expect(actualPage?.items[0].publicationStatus).toBe('PartiallyPublished');
  });

  it('includes the optional year and month params when both are provided', () => {
    const apiResponse: CalendarEventListPage = {
      items: [],
      page: 1,
      pageSize: 25,
      totalCount: 0,
      sort: 'timeZone',
      direction: 'asc',
    };

    let actualPage: CalendarEventListPage | undefined;
    service
      .list({
        page: 1,
        pageSize: 25,
        sort: 'timeZone',
        direction: 'asc',
        year: 2026,
        month: 6,
      })
      .subscribe((page) => {
        actualPage = page;
      });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events?page=1&pageSize=25&sort=timeZone&direction=asc&year=2026&month=6',
    );

    expect(request.request.method).toBe('GET');

    request.flush(apiResponse);

    expect(actualPage).toEqual(apiResponse);
  });

  it('posts a create request to the calendar events endpoint and returns the API response', () => {
    const createRequest: CreateCalendarEventRequest = {
      start: {
        localDateTime: '2026-06-06T10:00:00',
        timeZoneId: 'America/Vancouver',
      },
      texts: [
        {
          fieldKey: 'text1',
          value: 'English stream 1',
        },
        {
          fieldKey: 'text2',
          value: 'Description for stream 1',
        },
      ],
    };
    const apiResponse: CreateCalendarEventResponse = {
      calendarEventId: '6f9619ff8b864fb5bdfd4f5c2f2f16a1',
    };

    let actualResponse: CreateCalendarEventResponse | undefined;
    service.create(createRequest).subscribe((response) => {
      actualResponse = response;
    });

    const request = http.expectOne('https://api.example.test/api/calendar-events');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(createRequest);

    request.flush(apiResponse);

    expect(actualResponse).toEqual(apiResponse);
  });

  it('requests a single calendar event by id and returns it', () => {
    const apiResponse: CalendarEventDetailsResponse = {
      calendarEventId: '6f9619ff8b864fb5bdfd4f5c2f2f16a1',
      start: {
        localDateTime: '2026-06-06T10:00:00',
        timeZoneId: 'America/Vancouver',
      },
      scheduledStartUtc: '2026-06-06T17:00:00+00:00',
      displayTitle: 'English stream 1',
      canUpdate: true,
      canDelete: true,
      thumbnail: {
        fileName: 'stream.png',
        contentType: 'image/png',
        sizeBytes: 123,
        width: 1280,
        height: 720,
        updatedUtc: '2026-07-06T12:00:00+00:00',
      },
      canUpdateThumbnail: true,
      texts: [
        {
          fieldKey: 'text1',
          label: 'Title',
          type: 'ShortText',
          maxLength: 50,
          value: 'English stream 1',
        },
      ],
      platforms: [
        {
          platformId: 'platform-1',
          platformName: 'Main YouTube channel',
          platformType: 'YouTube',
          status: 'Failed',
          externalResourceId: 'uncertain-broadcast-id',
          thumbnailStatus: 'NotConfigured',
          publishedUtc: null,
          publicationUpdatedUtc: '2026-07-06T11:55:00+00:00',
          platformDeletedUtc: null,
          canPublish: true,
          canDeletePublication: false,
          canPreviewPublishingContent: true,
          canRecoverPublication: false,
        },
      ],
    };

    let actual: CalendarEventDetailsResponse | undefined;
    service.getById('6f9619ff8b864fb5bdfd4f5c2f2f16a1').subscribe((event) => {
      actual = event;
    });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events/6f9619ff8b864fb5bdfd4f5c2f2f16a1',
    );

    expect(request.request.method).toBe('GET');

    request.flush(apiResponse);

    expect(actual).toEqual(apiResponse);
  });

  it('uploads a thumbnail with the expected multipart part name', () => {
    const file = new File(['image-bytes'], 'stream.png', { type: 'image/png' });
    const apiResponse: CalendarEventThumbnail = {
      fileName: 'stream.png',
      contentType: 'image/png',
      sizeBytes: 11,
      width: 1280,
      height: 720,
      updatedUtc: '2026-07-06T12:00:00+00:00',
    };

    let actual: CalendarEventThumbnail | undefined;
    service.uploadThumbnail('event/id', file).subscribe((response) => {
      actual = response;
    });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events/event%2Fid/thumbnail',
    );

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toBeInstanceOf(FormData);
    expect((request.request.body as FormData).get('thumbnail')).toBe(file);

    request.flush(apiResponse);

    expect(actual).toEqual(apiResponse);
  });

  it('gets thumbnail bytes as a blob', () => {
    const blob = new Blob(['image-bytes'], { type: 'image/png' });

    let actual: Blob | undefined;
    service.getThumbnail('event/id').subscribe((response) => {
      actual = response;
    });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events/event%2Fid/thumbnail',
    );

    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');

    request.flush(blob);

    expect(actual).toBe(blob);
  });

  it('deletes a thumbnail through the thumbnail endpoint', () => {
    let completed = false;
    service.deleteThumbnail('event/id').subscribe({
      complete: () => {
        completed = true;
      },
    });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events/event%2Fid/thumbnail',
    );

    expect(request.request.method).toBe('DELETE');

    request.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
  });

  it('puts an update request to the by-id endpoint and returns the API response', () => {
    const updateRequest: UpdateCalendarEventRequest = {
      start: {
        localDateTime: '2026-07-20T09:30:00',
        timeZoneId: 'Europe/London',
      },
      texts: [
        {
          fieldKey: 'text1',
          value: 'Updated English title',
        },
        {
          fieldKey: 'text2',
          value: 'Updated description',
        },
      ],
    };
    const apiResponse: UpdateCalendarEventResponse = {
      calendarEventId: '6f9619ff8b864fb5bdfd4f5c2f2f16a1',
    };

    let actual: UpdateCalendarEventResponse | undefined;
    service.update('6f9619ff8b864fb5bdfd4f5c2f2f16a1', updateRequest).subscribe((response) => {
      actual = response;
    });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events/6f9619ff8b864fb5bdfd4f5c2f2f16a1',
    );

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(updateRequest);

    request.flush(apiResponse);

    expect(actual).toEqual(apiResponse);
  });

  it('posts a platform publish request to the event-platform publish endpoint', () => {
    const apiResponse: PublishPlatformResponse = {
      platformId: 'platform-1',
      platformName: 'Main YouTube channel',
      platformType: 'YouTube',
      status: 'Published',
      externalResourceId: 'broadcast-123',
      thumbnailStatus: 'Applied',
      publishedUtc: '2026-06-15T17:30:00+00:00',
      publicationUpdatedUtc: '2026-06-15T17:30:00+00:00',
      platformDeletedUtc: null,
      canPublish: false,
      canDeletePublication: true,
      canPreviewPublishingContent: true,
      canRecoverPublication: false,
    };

    let actualResponse: PublishPlatformResponse | undefined;
    service
      .publishPlatform('f81d4fae7dec11d0a76500a0c91e6bf6', 'platform-1')
      .subscribe((response) => {
        actualResponse = response;
      });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events/f81d4fae7dec11d0a76500a0c91e6bf6/platforms/platform-1/publish',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({});

    request.flush(apiResponse);

    expect(actualResponse).toEqual(apiResponse);
  });

  it('deletes a platform publication and returns the recomputed platform row', () => {
    const apiResponse: CalendarEventPlatform = {
      platformId: 'platform-1',
      platformName: 'Main YouTube channel',
      platformType: 'YouTube',
      status: 'NotPublished',
      externalResourceId: null,
      thumbnailStatus: 'NotConfigured',
      publishedUtc: null,
      publicationUpdatedUtc: null,
      platformDeletedUtc: null,
      canPublish: true,
      canDeletePublication: false,
      canPreviewPublishingContent: true,
      canRecoverPublication: false,
    };

    let actualResponse: CalendarEventPlatform | undefined;
    service
      .deletePlatformPublication('f81d4fae7dec11d0a76500a0c91e6bf6', 'platform-1')
      .subscribe((response) => {
        actualResponse = response;
      });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events/f81d4fae7dec11d0a76500a0c91e6bf6/platforms/platform-1/publication',
    );

    expect(request.request.method).toBe('DELETE');

    request.flush(apiResponse);

    expect(actualResponse).toEqual(apiResponse);
  });

  it('posts an empty body to recover a stale platform publication', () => {
    let completed = false;
    service
      .recoverPlatformPublication('event/id', 'platform id')
      .subscribe({ complete: () => (completed = true) });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events/event%2Fid/platforms/platform%20id/publication/recover',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({});
    request.flush(null);
    expect(completed).toBe(true);
  });

  it('gets row-level publishing content for an event platform', () => {
    const apiResponse = {
      type: 'Preview' as const,
      title: 'Rendered title',
      description: 'Rendered description',
    };

    let actualResponse: EventPlatformPublishingContent | undefined;
    service
      .getPublishingContent('f81d4fae7dec11d0a76500a0c91e6bf6', 'platform-1')
      .subscribe((response) => {
        actualResponse = response;
      });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events/f81d4fae7dec11d0a76500a0c91e6bf6/platforms/platform-1/publishing-content',
    );

    expect(request.request.method).toBe('GET');

    request.flush(apiResponse);

    expect(actualResponse).toEqual(apiResponse);
  });

  it('issues a DELETE to the by-id endpoint and completes with no body', () => {
    let completed = false;
    let emittedBody: unknown = 'unset';
    service.delete('6f9619ff8b864fb5bdfd4f5c2f2f16a1').subscribe({
      next: (value) => {
        emittedBody = value;
      },
      complete: () => {
        completed = true;
      },
    });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events/6f9619ff8b864fb5bdfd4f5c2f2f16a1',
    );

    expect(request.request.method).toBe('DELETE');

    request.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
    expect(emittedBody).toBeNull();
  });
});
