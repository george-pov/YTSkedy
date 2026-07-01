import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { testAppConfig } from 'src/app/shared/config/testing/app-config.fixture';
import {
  EventTextFieldsResponse,
  EventTextFieldsService,
  UpdateEventTextFieldsRequest,
} from './event-text-fields-service';

describe('EventTextFieldsService', () => {
  let http: HttpTestingController;
  let service: EventTextFieldsService;

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
    service = TestBed.inject(EventTextFieldsService);
  });

  afterEach(() => {
    http.verify();
  });

  it('gets the current event text fields setting', () => {
    const apiResponse: EventTextFieldsResponse = {
      fields: [
        {
          fieldKey: 'text1',
          label: 'Title',
          type: 'ShortText',
          maxLength: 50,
        },
        {
          fieldKey: 'text2',
          label: 'Description',
          type: 'LongText',
          maxLength: 2500,
        },
      ],
    };

    let actual: EventTextFieldsResponse | undefined;
    service.get().subscribe((response) => {
      actual = response;
    });

    const request = http.expectOne('https://api.example.test/api/settings/event-text-fields');

    expect(request.request.method).toBe('GET');

    request.flush(apiResponse);

    expect(actual).toEqual(apiResponse);
  });

  it('puts the updated field list and returns the normalized response', () => {
    const updateRequest: UpdateEventTextFieldsRequest = {
      fields: [
        {
          fieldKey: 'text9',
          label: 'Stream title',
          type: 'ShortText',
          maxLength: 80,
        },
      ],
    };
    const apiResponse: EventTextFieldsResponse = {
      fields: [
        {
          fieldKey: 'text1',
          label: 'Stream title',
          type: 'ShortText',
          maxLength: 80,
        },
      ],
    };

    let actual: EventTextFieldsResponse | undefined;
    service.update(updateRequest).subscribe((response) => {
      actual = response;
    });

    const request = http.expectOne('https://api.example.test/api/settings/event-text-fields');

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(updateRequest);

    request.flush(apiResponse);

    expect(actual).toEqual(apiResponse);
  });
});
