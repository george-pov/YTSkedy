import { provideRouter, Router } from '@angular/router';
import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { AuthFacade } from './auth-facade';
import { FakeAuthFacade } from './fake-auth-facade';
import { redirectAuthenticatedGuard } from './redirect-authenticated-guard';

@Component({ selector: 'spec-public-landing', template: '' })
class StubPublicLanding {}

@Component({ selector: 'spec-calendar-events', template: '' })
class StubCalendarEvents {}

function configure(fake: FakeAuthFacade) {
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      provideRouter([
        {
          path: '',
          component: StubPublicLanding,
          pathMatch: 'full',
          canActivate: [redirectAuthenticatedGuard],
        },
        {
          path: 'calendar-events',
          component: StubCalendarEvents,
        },
      ]),
      { provide: AuthFacade, useValue: fake },
    ],
  });
}

describe('redirectIfAuthenticatedGuard', () => {
  it('lets unauthenticated visitors render the public landing route', async () => {
    configure(new FakeAuthFacade({ authenticated: false }));

    const router = TestBed.inject(Router);
    const navigated = await router.navigateByUrl('/');

    expect(navigated).toBe(true);
    expect(router.url).toBe('/');
  });

  it('redirects authenticated visitors to /calendar-events', async () => {
    configure(new FakeAuthFacade({ authenticated: true }));

    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');

    expect(router.url).toBe('/calendar-events');
  });
});
