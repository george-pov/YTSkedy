import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { testAppConfig } from 'src/app/shared/config/testing/app-config.fixture';
import {
  EventTextField,
  EventTextFieldsResponse,
  EventTextFieldsService,
  UpdateEventTextFieldsRequest,
} from './event-text-fields-service';

describe('EventTextFieldsService', () => {
  const endpoint = 'https://api.example.test/api/settings/event-text-fields';

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

  it('gets the current event text fields setting', async () => {
    const apiResponse = eventTextFields();
    const responsePromise = firstValueFrom(service.get());
    const request = http.expectOne(endpoint);

    expect(request.request.method).toBe('GET');

    request.flush(apiResponse);

    await expect(responsePromise).resolves.toEqual(apiResponse);
  });

  it('puts the updated field list and returns the normalized response', async () => {
    const updateRequest: UpdateEventTextFieldsRequest = {
      fields: [eventTextField({ fieldKey: 'text9', label: 'Stream title', maxLength: 80 })],
    };
    const apiResponse = eventTextFields([
      eventTextField({ fieldKey: 'text1', label: 'Stream title', maxLength: 80 }),
    ]);

    const responsePromise = firstValueFrom(service.update(updateRequest));
    const request = http.expectOne(endpoint);

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(updateRequest);

    request.flush(apiResponse);

    await expect(responsePromise).resolves.toEqual(apiResponse);
  });

  function eventTextFields(
    fields: EventTextField[] = [
      eventTextField(),
      eventTextField({
        fieldKey: 'text2',
        label: 'Description',
        type: 'LongText',
        maxLength: 2500,
      }),
    ],
  ): EventTextFieldsResponse {
    return { fields };
  }

  function eventTextField(overrides: Partial<EventTextField> = {}): EventTextField {
    return {
      fieldKey: 'text1',
      label: 'Title',
      type: 'ShortText',
      maxLength: 50,
      ...overrides,
    };
  }
});
