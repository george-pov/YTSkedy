import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { testAppConfig } from 'src/app/shared/config/testing/app-config.fixture';
import {
  CalendarEvent,
  CalendarEventsService,
  CreateCalendarEventRequest,
  CreateCalendarEventResponse,
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

  it('requests calendar events by local calendar month and returns the API response', () => {
    const apiResponse: CalendarEvent[] = [
      {
        calendarEventId: 'calendar-event-1',
        start: {
          localDateTime: '2026-01-15T09:30:00',
          timeZoneId: 'Etc/UTC',
        },
        descriptions: [
          {
            language: 'en',
            title: 'Test stream',
            description: 'Synthetic API response fixture.',
          },
        ],
      },
    ];

    let actualEvents: CalendarEvent[] | undefined;
    service.listByMonth(2026, 6).subscribe((events) => {
      actualEvents = events;
    });

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events?year=2026&month=6',
    );

    expect(request.request.method).toBe('GET');

    request.flush(apiResponse);

    expect(actualEvents).toEqual(apiResponse);
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

    const request = http.expectOne(
      'https://api.example.test/api/calendar-events',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(createRequest);

    request.flush(apiResponse);

    expect(actualResponse).toEqual(apiResponse);
  });
});
