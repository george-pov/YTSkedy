import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import {
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { testAppConfig } from 'src/app/shared/config/testing/app-config.fixture';

import { AuthFacade } from './auth-facade';
import { bearerTokenInterceptor } from './bearer-token-interceptor';
import { FakeAuthFacade } from './fake-auth-facade';

const appConfig = testAppConfig();

const calendarEventsUrl =
  'https://api.example.test/api/calendar-events?year=2026&month=6';
const templatesUrl = 'https://api.example.test/api/templates';
const templateTokensUrl = 'https://api.example.test/api/template-tokens';
const unprotectedUrl = 'https://other.example.test/api/calendar-events';

class StubRouter {
  url = '/calendar-events';
}

describe('bearerTokenInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let facade: FakeAuthFacade;
  let router: StubRouter;

  beforeEach(() => {
    sessionStorage.clear();
    facade = new FakeAuthFacade({
      authenticated: true,
      apiToken: 'access-token-123',
    });
    router = new StubRouter();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([bearerTokenInterceptor])),
        provideHttpClientTesting(),
        { provide: APP_CONFIG, useValue: appConfig },
        { provide: AuthFacade, useValue: facade },
        { provide: Router, useValue: router },
      ],
    });

    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    try {
      controller.verify();
    } finally {
      sessionStorage.clear();
      TestBed.resetTestingModule();
    }
  });

  // `acquireApiToken` resolves in a microtask, so the HTTP request is not
  // dispatched until at least one Promise tick has flushed. Drain microtasks
  // here before asserting against the testing controller.
  async function flushMicrotasks(): Promise<void> {
    await Promise.resolve();
    await Promise.resolve();
  }

  it('attaches a bearer token to protected calendar events requests', async () => {
    http.get(calendarEventsUrl).subscribe();
    await flushMicrotasks();

    const request = controller.expectOne(calendarEventsUrl);

    expect(request.request.headers.get('Authorization')).toBe(
      'Bearer access-token-123',
    );
    expect(facade.acquireApiTokenCalls).toEqual([
      [
        appConfig.auth.calendarEventsReadScope,
        appConfig.auth.calendarEventsWriteScope,
      ],
    ]);

    request.flush([]);
  });

  it('attaches a bearer token to protected templates requests', async () => {
    http.get(templatesUrl).subscribe();
    await flushMicrotasks();

    const request = controller.expectOne(templatesUrl);

    expect(request.request.headers.get('Authorization')).toBe(
      'Bearer access-token-123',
    );
    expect(facade.acquireApiTokenCalls).toEqual([
      [
        appConfig.auth.calendarEventsReadScope,
        appConfig.auth.calendarEventsWriteScope,
      ],
    ]);

    request.flush({ templates: [] });
  });

  it('attaches a bearer token to protected template-tokens requests', async () => {
    http.get(templateTokensUrl).subscribe();
    await flushMicrotasks();

    const request = controller.expectOne(templateTokensUrl);

    expect(request.request.headers.get('Authorization')).toBe(
      'Bearer access-token-123',
    );

    request.flush({ tokens: [] });
  });

  it('leaves unprotected requests untouched and does not call the auth facade', () => {
    http.get(unprotectedUrl).subscribe();

    const request = controller.expectOne(unprotectedUrl);

    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(facade.acquireApiTokenCalls).toEqual([]);

    request.flush({});
  });

  it('triggers interactive sign-in once on 401 and propagates the error', async () => {
    router.url = '/calendar-events';

    const errorPromise = new Promise<HttpErrorResponse>((resolve) => {
      http.get(calendarEventsUrl).subscribe({
        next: () => resolve(new HttpErrorResponse({ status: 0 })),
        error: (err: HttpErrorResponse) => resolve(err),
      });
    });

    await flushMicrotasks();
    controller
      .expectOne(calendarEventsUrl)
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    const error = await errorPromise;

    expect(error.status).toBe(401);
    expect(facade.signInCalls).toEqual(['/calendar-events']);
    expect(sessionStorage.getItem('ytskedy.auth.recoveryInProgress')).toBe(
      'true',
    );
  });

  it('skips sign-in on a second 401 before any successful response (loop guard)', async () => {
    await respondWith401();
    expect(facade.signInCalls).toHaveLength(1);

    await respondWith401();
    expect(facade.signInCalls).toHaveLength(1);
  });

  it('clears the recovery flag on a successful protected response so a later 401 can retry', async () => {
    sessionStorage.setItem('ytskedy.auth.recoveryInProgress', 'true');

    await respondWithOk();

    expect(
      sessionStorage.getItem('ytskedy.auth.recoveryInProgress'),
    ).toBeNull();

    await respondWith401();

    expect(facade.signInCalls).toEqual(['/calendar-events']);
  });

  it('does not trigger sign-in on 403 and propagates the error', async () => {
    const errorPromise = new Promise<HttpErrorResponse>((resolve) => {
      http.get(calendarEventsUrl).subscribe({
        next: () => resolve(new HttpErrorResponse({ status: 0 })),
        error: (err: HttpErrorResponse) => resolve(err),
      });
    });

    await flushMicrotasks();
    controller
      .expectOne(calendarEventsUrl)
      .flush(null, { status: 403, statusText: 'Forbidden' });

    const error = await errorPromise;

    expect(error.status).toBe(403);
    expect(facade.signInCalls).toEqual([]);
    expect(
      sessionStorage.getItem('ytskedy.auth.recoveryInProgress'),
    ).toBeNull();
  });

  it('propagates token acquisition errors without making the HTTP request', async () => {
    facade.apiTokenError = new Error('silent token failed');

    const resultPromise = new Promise<unknown>((resolve) => {
      http.get(calendarEventsUrl).subscribe({
        next: () => resolve('next'),
        error: (err: unknown) => resolve(err),
      });
    });

    await flushMicrotasks();
    const result = await resultPromise;

    expect(result).toBeInstanceOf(Error);
    expect((result as Error).message).toBe('silent token failed');
    controller.expectNone(calendarEventsUrl);
  });

  async function respondWith401(): Promise<void> {
    await new Promise<void>((resolve) => {
      http.get(calendarEventsUrl).subscribe({
        next: () => resolve(),
        error: () => resolve(),
      });

      // Schedule the flush after the token-acquisition microtask resolves.
      void flushMicrotasks().then(() => {
        controller
          .expectOne(calendarEventsUrl)
          .flush(null, { status: 401, statusText: 'Unauthorized' });
      });
    });
  }

  async function respondWithOk(): Promise<void> {
    await new Promise<void>((resolve) => {
      http.get(calendarEventsUrl).subscribe({
        next: () => resolve(),
        error: () => resolve(),
      });

      void flushMicrotasks().then(() => {
        controller.expectOne(calendarEventsUrl).flush([]);
      });
    });
  }
});
