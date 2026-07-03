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
  PlatformReferenceKeyConflictError,
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
          referenceKey: 'youTube1',
          type: 'YouTube',
          publishSettings: {
            credentials: {
              clientId: 'client-id',
              clientSecretConfigured: true,
              refreshTokenConfigured: true,
              clientSecretDisplayValue: '*********A3B',
              refreshTokenDisplayValue: '*********Z9Y',
            },
            privacyStatus: 'private',
            selfDeclaredMadeForKids: false,
          },
          publishingContent: {
            titleTemplateId: 'youtube-title-template',
            descriptionTemplateId: 'youtube-description-template',
          },
        },
        {
          platformId: '5aa4a32f3f344de1a7c3a9f4a2f94918',
          name: 'Company blog',
          referenceKey: null,
          type: 'WordPress',
          publishSettings: {
            siteUrl: 'https://blog.example.test/',
            username: 'publisher',
            postStatus: 'draft',
            applicationPasswordConfigured: true,
            passwordDisplayValue: '*******',
          },
          publishingContent: {
            titleTemplateId: 'wordpress-title-template',
            descriptionTemplateId: 'wordpress-description-template',
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
          referenceKey: 'youTube1',
          type: 'YouTube',
          publishSettings: {
            credentials: {
              clientId: 'client-id',
              clientSecretConfigured: true,
              refreshTokenConfigured: true,
              clientSecretDisplayValue: '*********A3B',
              refreshTokenDisplayValue: '*********Z9Y',
            },
            privacyStatus: 'private',
            selfDeclaredMadeForKids: false,
          },
          publishingContent: {
            titleTemplateId: 'youtube-title-template',
            descriptionTemplateId: 'youtube-description-template',
          },
        },
        {
          id: '5aa4a32f3f344de1a7c3a9f4a2f94918',
          name: 'Company blog',
          referenceKey: null,
          type: 'WordPress',
          publishSettings: {
            siteUrl: 'https://blog.example.test/',
            username: 'publisher',
            postStatus: 'draft',
            applicationPasswordConfigured: true,
            passwordDisplayValue: '*******',
          },
          publishingContent: {
            titleTemplateId: 'wordpress-title-template',
            descriptionTemplateId: 'wordpress-description-template',
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

  it('maps a missing API reference key to null', async () => {
    const responsePromise = firstValueFrom(service.list());

    const request = http.expectOne('https://api.example.test/api/platforms');

    request.flush({
      items: [
        {
          platformId: '4fb4a32f3f344de1a7c3a9f4a2f94918',
          name: 'Legacy channel',
          type: 'YouTube',
          publishingContent: publishingContent(),
        },
      ],
    });

    await expect(responsePromise).resolves.toEqual({
      platforms: [
        {
          id: '4fb4a32f3f344de1a7c3a9f4a2f94918',
          name: 'Legacy channel',
          referenceKey: null,
          type: 'YouTube',
          publishingContent: publishingContent(),
        },
      ],
    });
  });

  it('posts a create request to the platforms endpoint and maps the created platform', async () => {
    const createRequest: CreatePlatformRequest = {
      name: 'Second channel',
      referenceKey: 'youTube1',
      type: 'YouTube',
      publishSettings: {
        credentials: {
          clientId: 'second-client-id',
          clientSecret: 'second-client-secret',
          refreshToken: 'second-refresh-token',
        },
        privacyStatus: 'public',
        selfDeclaredMadeForKids: true,
      },
      publishingContent: {
        titleTemplateId: 'youtube-title-template',
        descriptionTemplateId: 'youtube-description-template',
      },
    };

    const responsePromise = firstValueFrom(service.create(createRequest));

    const request = http.expectOne('https://api.example.test/api/platforms');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(createRequest);

    request.flush({
      platformId: '9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d',
      name: 'Second channel',
      referenceKey: 'youTube1',
      type: 'YouTube',
      publishSettings: {
        credentials: {
          clientId: 'second-client-id',
          clientSecretConfigured: true,
          refreshTokenConfigured: true,
          clientSecretDisplayValue: '*********C4D',
          refreshTokenDisplayValue: '*********R8S',
        },
        privacyStatus: 'public',
        selfDeclaredMadeForKids: true,
      },
      publishingContent: {
        titleTemplateId: 'youtube-title-template',
        descriptionTemplateId: 'youtube-description-template',
      },
    });

    await expect(responsePromise).resolves.toEqual({
      id: '9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d',
      name: 'Second channel',
      referenceKey: 'youTube1',
      type: 'YouTube',
      publishSettings: {
        credentials: {
          clientId: 'second-client-id',
          clientSecretConfigured: true,
          refreshTokenConfigured: true,
          clientSecretDisplayValue: '*********C4D',
          refreshTokenDisplayValue: '*********R8S',
        },
        privacyStatus: 'public',
        selfDeclaredMadeForKids: true,
      },
      publishingContent: {
        titleTemplateId: 'youtube-title-template',
        descriptionTemplateId: 'youtube-description-template',
      },
    });
  });

  it('posts a WordPress create request and maps the redacted created platform', async () => {
    const createRequest: CreatePlatformRequest = {
      name: 'Company blog',
      referenceKey: 'blog-1',
      type: 'WordPress',
      publishSettings: {
        siteUrl: 'https://blog.example.test/',
        username: 'publisher',
        postStatus: 'draft',
        applicationPassword: 'local-test-password',
      },
      publishingContent: {
        titleTemplateId: 'wordpress-title-template',
        descriptionTemplateId: 'wordpress-description-template',
      },
    };

    const responsePromise = firstValueFrom(service.create(createRequest));

    const request = http.expectOne('https://api.example.test/api/platforms');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(createRequest);

    request.flush({
      platformId: '5aa4a32f3f344de1a7c3a9f4a2f94918',
      name: 'Company blog',
      referenceKey: 'blog-1',
      type: 'WordPress',
      publishSettings: {
        siteUrl: 'https://blog.example.test/',
        username: 'publisher',
        postStatus: 'draft',
        applicationPasswordConfigured: true,
        passwordDisplayValue: '*******',
      },
      publishingContent: {
        titleTemplateId: 'wordpress-title-template',
        descriptionTemplateId: 'wordpress-description-template',
      },
    });

    await expect(responsePromise).resolves.toEqual({
      id: '5aa4a32f3f344de1a7c3a9f4a2f94918',
      name: 'Company blog',
      referenceKey: 'blog-1',
      type: 'WordPress',
      publishSettings: {
        siteUrl: 'https://blog.example.test/',
        username: 'publisher',
        postStatus: 'draft',
        applicationPasswordConfigured: true,
        passwordDisplayValue: '*******',
      },
      publishingContent: {
        titleTemplateId: 'wordpress-title-template',
        descriptionTemplateId: 'wordpress-description-template',
      },
    });
  });

  it('maps duplicate-name create responses to a typed conflict error', async () => {
    const responsePromise = firstValueFrom(
      service.create({
        name: 'Main YouTube channel',
        type: 'YouTube',
        publishSettings: {
          credentials: {
            clientId: 'client-id',
            clientSecret: 'client-secret',
            refreshToken: 'refresh-token',
          },
          privacyStatus: 'private',
          selfDeclaredMadeForKids: false,
        },
        publishingContent: publishingContent(),
      }),
    );

    const request = http.expectOne('https://api.example.test/api/platforms');

    request.flush('A platform named already exists.', {
      status: 409,
      statusText: 'Conflict',
    });

    await expect(responsePromise).rejects.toBeInstanceOf(PlatformNameConflictError);
  });

  it('maps duplicate-reference-key create responses to a typed conflict error', async () => {
    const responsePromise = firstValueFrom(
      service.create({
        name: 'Second channel',
        referenceKey: 'youTube1',
        type: 'YouTube',
        publishSettings: {
          credentials: {
            clientId: 'client-id',
            clientSecret: 'client-secret',
            refreshToken: 'refresh-token',
          },
          privacyStatus: 'private',
          selfDeclaredMadeForKids: false,
        },
        publishingContent: publishingContent(),
      }),
    );

    const request = http.expectOne('https://api.example.test/api/platforms');

    request.flush("A platform reference key 'youTube1' already exists.", {
      status: 409,
      statusText: 'Conflict',
    });

    await expect(responsePromise).rejects.toBeInstanceOf(PlatformReferenceKeyConflictError);
  });

  it('puts an update request to the by-id route and maps the updated platform', async () => {
    const updateRequest: UpdatePlatformRequest = {
      name: 'Renamed channel',
      referenceKey: null,
      publishSettings: {
        credentials: {
          clientId: 'renamed-client-id',
        },
        privacyStatus: 'unlisted',
        selfDeclaredMadeForKids: false,
      },
      publishingContent: {
        titleTemplateId: 'updated-title-template',
        descriptionTemplateId: 'updated-description-template',
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
      referenceKey: null,
      type: 'YouTube',
      publishSettings: {
        credentials: {
          clientId: 'renamed-client-id',
          clientSecretConfigured: true,
          refreshTokenConfigured: true,
          clientSecretDisplayValue: '*********N3W',
          refreshTokenDisplayValue: '*********T0K',
        },
        privacyStatus: 'unlisted',
        selfDeclaredMadeForKids: false,
      },
      publishingContent: {
        titleTemplateId: 'updated-title-template',
        descriptionTemplateId: 'updated-description-template',
      },
    });

    await expect(responsePromise).resolves.toEqual({
      id: '4fb4a32f3f344de1a7c3a9f4a2f94918',
      name: 'Renamed channel',
      referenceKey: null,
      type: 'YouTube',
      publishSettings: {
        credentials: {
          clientId: 'renamed-client-id',
          clientSecretConfigured: true,
          refreshTokenConfigured: true,
          clientSecretDisplayValue: '*********N3W',
          refreshTokenDisplayValue: '*********T0K',
        },
        privacyStatus: 'unlisted',
        selfDeclaredMadeForKids: false,
      },
      publishingContent: {
        titleTemplateId: 'updated-title-template',
        descriptionTemplateId: 'updated-description-template',
      },
    });
  });

  it('puts a WordPress update request and maps the redacted updated platform', async () => {
    const updateRequest: UpdatePlatformRequest = {
      name: 'Company blog',
      referenceKey: 'blog-1',
      publishSettings: {
        siteUrl: 'https://blog.example.test/',
        username: 'publisher',
        postStatus: 'publish',
      },
      publishingContent: publishingContent(),
    };

    const responsePromise = firstValueFrom(
      service.update('WordPress', '5aa4a32f3f344de1a7c3a9f4a2f94918', updateRequest),
    );

    const request = http.expectOne(
      'https://api.example.test/api/platforms/5aa4a32f3f344de1a7c3a9f4a2f94918',
    );

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(updateRequest);

    request.flush({
      platformId: '5aa4a32f3f344de1a7c3a9f4a2f94918',
      name: 'Company blog',
      referenceKey: 'blog-1',
      type: 'WordPress',
      publishSettings: {
        siteUrl: 'https://blog.example.test/',
        username: 'publisher',
        postStatus: 'publish',
        applicationPasswordConfigured: true,
        passwordDisplayValue: '*******',
      },
      publishingContent: publishingContent(),
    });

    await expect(responsePromise).resolves.toEqual({
      id: '5aa4a32f3f344de1a7c3a9f4a2f94918',
      name: 'Company blog',
      referenceKey: 'blog-1',
      type: 'WordPress',
      publishSettings: {
        siteUrl: 'https://blog.example.test/',
        username: 'publisher',
        postStatus: 'publish',
        applicationPasswordConfigured: true,
        passwordDisplayValue: '*******',
      },
      publishingContent: publishingContent(),
    });
  });

  it('maps duplicate-name update responses to a typed conflict error', async () => {
    const responsePromise = firstValueFrom(
      service.update('YouTube', '4fb4a32f3f344de1a7c3a9f4a2f94918', {
        name: 'Main YouTube channel',
        publishSettings: {
          credentials: {
            clientId: 'client-id',
          },
          privacyStatus: 'private',
          selfDeclaredMadeForKids: false,
        },
        publishingContent: publishingContent(),
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

  it('maps duplicate-reference-key update responses to a typed conflict error', async () => {
    const responsePromise = firstValueFrom(
      service.update('YouTube', '4fb4a32f3f344de1a7c3a9f4a2f94918', {
        name: 'Main YouTube channel',
        referenceKey: 'youTube1',
        publishSettings: {
          credentials: {
            clientId: 'client-id',
          },
          privacyStatus: 'private',
          selfDeclaredMadeForKids: false,
        },
        publishingContent: publishingContent(),
      }),
    );

    const request = http.expectOne(
      'https://api.example.test/api/platforms/4fb4a32f3f344de1a7c3a9f4a2f94918',
    );

    request.flush("A platform reference key 'youTube1' already exists.", {
      status: 409,
      statusText: 'Conflict',
    });

    await expect(responsePromise).rejects.toBeInstanceOf(PlatformReferenceKeyConflictError);
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

  function publishingContent() {
    return {
      titleTemplateId: 'title-template',
      descriptionTemplateId: 'description-template',
    };
  }
});
