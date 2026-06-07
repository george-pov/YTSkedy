import { TestBed } from '@angular/core/testing';
import {
  EnvironmentInjector,
  runInInjectionContext,
} from '@angular/core';
import {
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
} from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';

import { AuthFacade } from './auth-facade';
import { authenticatedGuard } from './authenticated-guard';
import { FakeAuthFacade } from './fake-auth-facade';

function runGuard(targetUrl: string): boolean {
  const route = {} as ActivatedRouteSnapshot;
  const state = { url: targetUrl } as RouterStateSnapshot;
  const injector = TestBed.inject(EnvironmentInjector);

  return runInInjectionContext(
    injector,
    () => authenticatedGuard(route, state) as boolean,
  );
}

describe('authenticatedGuard', () => {
  let fake: FakeAuthFacade;

  function configure(authenticated: boolean) {
    fake = new FakeAuthFacade({ authenticated });
    TestBed.configureTestingModule({
      providers: [{ provide: AuthFacade, useValue: fake }],
    });
  }

  beforeEach(() => {
    fake = new FakeAuthFacade();
  });

  it('allows authenticated users to activate the route', () => {
    configure(true);

    const allowed = runGuard('/calendar-events');

    expect(allowed).toBe(true);
    expect(fake.signInCalls).toEqual([]);
  });

  it('blocks unauthenticated users and triggers sign-in with the requested URL', () => {
    configure(false);

    const allowed = runGuard('/calendar-events');

    expect(allowed).toBe(false);
    expect(fake.signInCalls).toEqual(['/calendar-events']);
  });
});
