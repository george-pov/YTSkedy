import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { testAppConfig } from 'src/app/shared/config/testing/app-config.fixture';
import {
  CreatePlatformRequest,
  PlatformListResponse,
  PlatformNameConflictError,
  PlatformsService,
  UpdatePlatformRequest,
} from './platforms-service';

describe('PlatformsService', () => {
  let http: HttpTestingController;
  let service: PlatformsService;

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
    service = TestBed.inject(PlatformsService);
  });

  afterEach(() => {
    http.verify();
  });

  it('requests all platforms and maps the API envelope to the page model', async () => {
    const responsePromise = firstValueFrom(service.list());

    const request = http.expectOne('https://api.example.test/api/platforms');

    expect(request.request.method).toBe('GET');

    request.flush({
      items: [
        {
          platformId: '4fb4a32f3f344de1a7c3a9f4a2f94918',
          name: 'Main YouTube channel',
          type: 'YouTube',
          publishSettings: {
            credentials: 'main-youtube-channel',
            privacyStatus: 'private',
            selfDeclaredMadeForKids: false,
          },
        },
      ],
    });

    const response = await responsePromise;
    const expected: PlatformListResponse = {
      platforms: [
        {
          id: '4fb4a32f3f344de1a7c3a9f4a2f94918',
          name: 'Main YouTube channel',
          type: 'YouTube',
          publishSettings: {
            credentials: 'main-youtube-channel',
            privacyStatus: 'private',
            selfDeclaredMadeForKids: false,
          },
        },
      ],
    };

    expect(response).toEqual(expected);
  });

  it('includes the optional type query parameter when filtering by type', () => {
    service.list('YouTube').subscribe();

    const request = http.expectOne('https://api.example.test/api/platforms?type=YouTube');

    expect(request.request.method).toBe('GET');

    request.flush({ items: [] });
  });

  it('posts a create request to the platforms endpoint and maps the created platform', async () => {
    const createRequest: CreatePlatformRequest = {
      name: 'Second channel',
      type: 'YouTube',
      publishSettings: {
        credentials: 'second-channel',
        privacyStatus: 'public',
        selfDeclaredMadeForKids: true,
      },
    };

    const responsePromise = firstValueFrom(service.create(createRequest));

    const request = http.expectOne('https://api.example.test/api/platforms');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(createRequest);

    request.flush({
      platformId: '9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d',
      name: 'Second channel',
      type: 'YouTube',
      publishSettings: {
        credentials: 'second-channel',
        privacyStatus: 'public',
        selfDeclaredMadeForKids: true,
      },
    });

    await expect(responsePromise).resolves.toEqual({
      id: '9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d',
      name: 'Second channel',
      type: 'YouTube',
      publishSettings: {
        credentials: 'second-channel',
        privacyStatus: 'public',
        selfDeclaredMadeForKids: true,
      },
    });
  });

  it('maps duplicate-name create responses to a typed conflict error', async () => {
    const responsePromise = firstValueFrom(
      service.create({
        name: 'Main YouTube channel',
        type: 'YouTube',
        publishSettings: {
          credentials: 'main-youtube-channel',
          privacyStatus: 'private',
          selfDeclaredMadeForKids: false,
        },
      }),
    );

    const request = http.expectOne('https://api.example.test/api/platforms');

    request.flush('A platform named already exists.', {
      status: 409,
      statusText: 'Conflict',
    });

    await expect(responsePromise).rejects.toBeInstanceOf(PlatformNameConflictError);
  });

  it('puts an update request to the by-id route and maps the updated platform', async () => {
    const updateRequest: UpdatePlatformRequest = {
      name: 'Renamed channel',
      publishSettings: {
        credentials: 'renamed-channel',
        privacyStatus: 'unlisted',
        selfDeclaredMadeForKids: false,
      },
    };

    const responsePromise = firstValueFrom(
      service.update('YouTube', '4fb4a32f3f344de1a7c3a9f4a2f94918', updateRequest),
    );

    const request = http.expectOne(
      'https://api.example.test/api/platforms/4fb4a32f3f344de1a7c3a9f4a2f94918',
    );

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(updateRequest);

    request.flush({
      platformId: '4fb4a32f3f344de1a7c3a9f4a2f94918',
      name: 'Renamed channel',
      type: 'YouTube',
      publishSettings: {
        credentials: 'renamed-channel',
        privacyStatus: 'unlisted',
        selfDeclaredMadeForKids: false,
      },
    });

    await expect(responsePromise).resolves.toEqual({
      id: '4fb4a32f3f344de1a7c3a9f4a2f94918',
      name: 'Renamed channel',
      type: 'YouTube',
      publishSettings: {
        credentials: 'renamed-channel',
        privacyStatus: 'unlisted',
        selfDeclaredMadeForKids: false,
      },
    });
  });

  it('maps duplicate-name update responses to a typed conflict error', async () => {
    const responsePromise = firstValueFrom(
      service.update('YouTube', '4fb4a32f3f344de1a7c3a9f4a2f94918', {
        name: 'Main YouTube channel',
        publishSettings: {
          credentials: 'main-youtube-channel',
          privacyStatus: 'private',
          selfDeclaredMadeForKids: false,
        },
      }),
    );

    const request = http.expectOne(
      'https://api.example.test/api/platforms/4fb4a32f3f344de1a7c3a9f4a2f94918',
    );

    request.flush("A platform named 'Main YouTube channel' already exists.", {
      status: 409,
      statusText: 'Conflict',
    });

    await expect(responsePromise).rejects.toBeInstanceOf(PlatformNameConflictError);
  });

  it('issues a DELETE to the by-id route and completes with no body', async () => {
    const responsePromise = firstValueFrom(
      service.delete('YouTube', '4fb4a32f3f344de1a7c3a9f4a2f94918'),
    );

    const request = http.expectOne(
      'https://api.example.test/api/platforms/4fb4a32f3f344de1a7c3a9f4a2f94918',
    );

    expect(request.request.method).toBe('DELETE');

    request.flush(null, { status: 204, statusText: 'No Content' });

    await expect(responsePromise).resolves.toBeNull();
  });
});
