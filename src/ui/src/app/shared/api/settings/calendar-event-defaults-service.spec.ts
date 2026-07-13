import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { testAppConfig } from 'src/app/shared/config/testing/app-config.fixture';
import {
  CalendarEventDefaultsResponse,
  CalendarEventDefaultsService,
  UpdateCalendarEventDefaultsRequest,
} from './calendar-event-defaults-service';

describe('CalendarEventDefaultsService', () => {
  const endpoint = 'https://api.example.test/api/settings/calendar-event-defaults';
  let http: HttpTestingController;
  let service: CalendarEventDefaultsService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: APP_CONFIG,
          useValue: testAppConfig({ api: { baseUrl: 'https://api.example.test' } }),
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(CalendarEventDefaultsService);
  });

  afterEach(() => http.verify());

  it('gets both settings sections', async () => {
    const response = defaults();
    const promise = firstValueFrom(service.get());
    const request = http.expectOne(endpoint);

    expect(request.request.method).toBe('GET');
    request.flush(response);

    await expect(promise).resolves.toEqual(response);
  });

  it('puts the complete replacement', async () => {
    const replacement: UpdateCalendarEventDefaultsRequest = defaults({
      startDefaults: {
        dayOfWeek: 'Sunday',
        localTime: '10:00',
        timeZoneId: 'America/Vancouver',
      },
    });
    const promise = firstValueFrom(service.update(replacement));
    const request = http.expectOne(endpoint);

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(replacement);
    request.flush(replacement);

    await expect(promise).resolves.toEqual(replacement);
  });

  function defaults(
    overrides: Partial<CalendarEventDefaultsResponse> = {},
  ): CalendarEventDefaultsResponse {
    return {
      eventTextFields: {
        fields: [
          {
            fieldKey: 'text1',
            label: 'Title',
            type: 'ShortText',
            maxLength: 50,
          },
        ],
      },
      startDefaults: { dayOfWeek: null, localTime: null, timeZoneId: null },
      ...overrides,
    };
  }
});
