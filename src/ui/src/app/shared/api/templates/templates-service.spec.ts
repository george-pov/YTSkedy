import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { testAppConfig } from 'src/app/shared/config/testing/app-config.fixture';
import {
  CreateTemplateRequest,
  CreateTemplateResponse,
  TemplateListResponse,
  TemplatesService,
  TemplateTokenListResponse,
  UpdateTemplateRequest,
  UpdateTemplateResponse,
} from './templates-service';

describe('TemplatesService', () => {
  let http: HttpTestingController;
  let service: TemplatesService;

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
    service = TestBed.inject(TemplatesService);
  });

  afterEach(() => {
    http.verify();
  });

  it('requests all templates and returns the envelope', () => {
    const apiResponse: TemplateListResponse = {
      templates: [
        {
          id: '9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d',
          name: 'Weeknight stream',
          type: 'YouTube',
          content: 'Live at {{ localizedTime }} on {{ localizedDate }}',
        },
      ],
    };

    let actual: TemplateListResponse | undefined;
    service.list().subscribe((response) => {
      actual = response;
    });

    const request = http.expectOne('https://api.example.test/api/templates');

    expect(request.request.method).toBe('GET');

    request.flush(apiResponse);

    expect(actual).toEqual(apiResponse);
  });

  it('includes the optional type query parameter when filtering by type', () => {
    const apiResponse: TemplateListResponse = { templates: [] };

    service.list('WordPress').subscribe();

    const request = http.expectOne('https://api.example.test/api/templates?type=WordPress');

    expect(request.request.method).toBe('GET');

    request.flush(apiResponse);
  });

  it('posts a create request to the templates endpoint and returns the new id', () => {
    const createRequest: CreateTemplateRequest = {
      name: 'Weeknight stream',
      type: 'YouTube',
      content: 'Live at {{ localizedTime }}',
    };
    const apiResponse: CreateTemplateResponse = {
      id: '9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d',
      name: 'Weeknight stream',
      type: 'YouTube',
    };

    let actual: CreateTemplateResponse | undefined;
    service.create(createRequest).subscribe((response) => {
      actual = response;
    });

    const request = http.expectOne('https://api.example.test/api/templates');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(createRequest);

    request.flush(apiResponse);

    expect(actual).toEqual(apiResponse);
  });

  it('puts an update request to the type-and-id route with only name and content', () => {
    const updateRequest: UpdateTemplateRequest = {
      name: 'Weeknight stream (edited)',
      content: 'Live at {{ localizedTime }}',
    };
    const apiResponse: UpdateTemplateResponse = {
      id: '9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d',
      name: 'Weeknight stream (edited)',
      type: 'YouTube',
    };

    let actual: UpdateTemplateResponse | undefined;
    service
      .update('YouTube', '9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d', updateRequest)
      .subscribe((response) => {
        actual = response;
      });

    const request = http.expectOne(
      'https://api.example.test/api/templates/YouTube/9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d',
    );

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(updateRequest);

    request.flush(apiResponse);

    expect(actual).toEqual(apiResponse);
  });

  it('issues a DELETE to the type-and-id route and completes with no body', () => {
    let completed = false;
    service.delete('WordPress', '9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d').subscribe({
      complete: () => {
        completed = true;
      },
    });

    const request = http.expectOne(
      'https://api.example.test/api/templates/WordPress/9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d',
    );

    expect(request.request.method).toBe('DELETE');

    request.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
  });

  it('requests the template token catalog and returns the envelope', () => {
    const apiResponse: TemplateTokenListResponse = {
      tokens: [{ name: 'localizedDate' }, { name: 'localizedTime' }],
    };

    let actual: TemplateTokenListResponse | undefined;
    service.listTokens().subscribe((response) => {
      actual = response;
    });

    const request = http.expectOne('https://api.example.test/api/template-tokens');

    expect(request.request.method).toBe('GET');

    request.flush(apiResponse);

    expect(actual).toEqual(apiResponse);
  });
});
