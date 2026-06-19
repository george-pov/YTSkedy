import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { testAppConfig } from 'src/app/shared/config/testing/app-config.fixture';
import {
  CalendarEvent,
  CalendarEventListPage,
  CalendarEventsService,
  CreateCalendarEventRequest,
  CreateCalendarEventResponse,
  PublishYouTubeResponse,
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

  it('requests a calendar events page with the paging and sorting query and returns the envelope', () => {
    const apiResponse: CalendarEventListPage = {
      items: [
        {
          calendarEventId: 'calendar-event-1',
          start: {
            localDateTime: '2026-01-15T09:30:00',
            timeZoneId: 'Etc/UTC',
          },
          scheduledStartUtc: '2026-01-15T09:30:00+00:00',
          descriptions: [
            {
              language: 'en',
              title: 'Test stream',
              description: 'Synthetic API response fixture.',
            },
          ],
          status: 'Draft',
          canPublish: true,
          canUpdate: true,
          canDelete: true,
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
  });

  it('includes the optional year and month params when both are provided', () => {
    const apiResponse: CalendarEventListPage = {
      items: [],
      page: 1,
      pageSize: 25,
      totalCount: 0,
      sort: 'status',
      direction: 'asc',
    };

    let actualPage: CalendarEventListPage | undefined;
    service
      .list({
        page: 1,
        pageSize: 25,
        sort: 'status',
        direction: 'asc',
        year: 2026,
        month: 6,
      })
      .subscribe((page) => {
        actualPage = page;
      });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events?page=1&pageSize=25&sort=status&direction=asc&year=2026&month=6',
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
      descriptions: [
        {
          language: 'en',
          title: 'English stream 1',
          description: 'Description for stream 1 in English',
        },
        {
          language: 'ru',
          title: 'Russian stream 1',
          description: 'Description for stream 1 in Russian',
        },
      ],
    };
    const apiResponse: CreateCalendarEventResponse = {
      calendarEventId: '20260606T170000Z',
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
    const apiResponse: CalendarEvent = {
      calendarEventId: '20260606T170000Z',
      start: {
        localDateTime: '2026-06-06T10:00:00',
        timeZoneId: 'America/Vancouver',
      },
      scheduledStartUtc: '2026-06-06T17:00:00+00:00',
      descriptions: [
        {
          language: 'en',
          title: 'English stream 1',
          description: 'Description for stream 1 in English',
        },
      ],
      status: 'Draft',
      canPublish: false,
      canUpdate: true,
      canDelete: false,
    };

    let actual: CalendarEvent | undefined;
    service.getById('20260606T170000Z').subscribe((event) => {
      actual = event;
    });

    const request = http.expectOne('https://api.example.test/api/calendar-events/20260606T170000Z');

    expect(request.request.method).toBe('GET');

    request.flush(apiResponse);

    expect(actual).toEqual(apiResponse);
  });

  it('puts an update request to the by-id endpoint and returns the API response', () => {
    const updateRequest: UpdateCalendarEventRequest = {
      descriptions: [
        {
          language: 'en',
          title: 'Updated English title',
          description: 'Updated English description',
        },
        {
          language: 'ru',
          title: 'Updated Russian title',
          description: 'Updated Russian description',
        },
      ],
    };
    const apiResponse: UpdateCalendarEventResponse = {
      calendarEventId: '20260606T170000Z',
    };

    let actual: UpdateCalendarEventResponse | undefined;
    service.update('20260606T170000Z', updateRequest).subscribe((response) => {
      actual = response;
    });

    const request = http.expectOne('https://api.example.test/api/calendar-events/20260606T170000Z');

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(updateRequest);

    request.flush(apiResponse);

    expect(actual).toEqual(apiResponse);
  });

  it('posts a publish request to the calendar event publish endpoint and returns the API response', () => {
    const apiResponse: PublishYouTubeResponse = {
      calendarEventId: '20260615T170000Z',
      status: 'Published',
      youTubeBroadcastId: 'broadcast-123',
    };

    let actualResponse: PublishYouTubeResponse | undefined;
    service.publish('20260615T170000Z').subscribe((response) => {
      actualResponse = response;
    });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events/20260615T170000Z/publish',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({});

    request.flush(apiResponse);

    expect(actualResponse).toEqual(apiResponse);
  });

  it('issues a DELETE to the by-id endpoint and completes with no body', () => {
    let completed = false;
    let emittedBody: unknown = 'unset';
    service.delete('20260606T170000Z').subscribe({
      next: (value) => {
        emittedBody = value;
      },
      complete: () => {
        completed = true;
      },
    });

    const request = http.expectOne('https://api.example.test/api/calendar-events/20260606T170000Z');

    expect(request.request.method).toBe('DELETE');

    request.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
    expect(emittedBody).toBeNull();
  });
});
