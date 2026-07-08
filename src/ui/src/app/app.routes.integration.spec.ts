import { provideRouter, Router, RouterOutlet } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import {
  Component,
  EnvironmentInjector,
  provideZonelessChangeDetection,
  runInInjectionContext,
} from '@angular/core';
import { beforeEach, describe, expect, it } from 'vitest';

import { AuthFacade } from './shared/auth/auth-facade';
import { authenticatedGuard } from './shared/auth/authenticated-guard';
import { FakeAuthFacade } from './shared/auth/fake-auth-facade';
import {
  pendingChangesGuard,
  type PendingChangesAware,
} from './shared/routing/pending-changes-guard';

// Stand-in components keep this spec MSAL-free and avoid pulling page
// components (which would drag in services we don't care about here).
// Distinct selectors silence Angular's NG0912 component-ID collision
// warning, which fires when multiple stub components share an
// indistinguishable template.
@Component({ selector: 'spec-stub-home', template: '' })
class StubHome {}

@Component({ selector: 'spec-stub-calendar-events', template: '' })
class StubCalendarEvents {}

@Component({ selector: 'spec-stub-calendar-event-details', template: '' })
class StubCalendarEventDetails implements PendingChangesAware {
  static canDeactivate = true;

  canDeactivateWithPendingChanges(): boolean {
    return StubCalendarEventDetails.canDeactivate;
  }
}

@Component({
  selector: 'spec-stub-shell',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
class StubShell {}

function configure(fake: FakeAuthFacade) {
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      provideRouter([
        {
          path: '',
          component: StubHome,
          pathMatch: 'full',
        },
        {
          path: 'calendar-events',
          component: StubCalendarEvents,
          canActivate: [authenticatedGuard],
        },
        {
          path: 'calendar-events/:calendarEventId/edit',
          component: StubCalendarEventDetails,
          canActivate: [authenticatedGuard],
          canDeactivate: [pendingChangesGuard],
        },
      ]),
      { provide: AuthFacade, useValue: fake },
    ],
  });
}

describe('authenticatedGuard on /calendar-events', () => {
  let fake: FakeAuthFacade;

  beforeEach(() => {
    fake = new FakeAuthFacade();
    StubCalendarEventDetails.canDeactivate = true;
  });

  it('blocks unauthenticated navigation and triggers sign-in with the requested URL', async () => {
    fake = new FakeAuthFacade({ authenticated: false });
    configure(fake);

    const router = TestBed.inject(Router);
    const injector = TestBed.inject(EnvironmentInjector);

    const succeeded = await runInInjectionContext(injector, () =>
      router.navigateByUrl('/calendar-events'),
    );

    expect(succeeded).toBe(false);
    expect(fake.signInCalls).toEqual(['/calendar-events']);
  });

  it('permits authenticated navigation to /calendar-events without calling sign-in', async () => {
    fake = new FakeAuthFacade({ authenticated: true });
    configure(fake);

    const router = TestBed.inject(Router);
    const injector = TestBed.inject(EnvironmentInjector);

    const succeeded = await runInInjectionContext(injector, () =>
      router.navigateByUrl('/calendar-events'),
    );

    expect(succeeded).toBe(true);
    expect(fake.signInCalls).toEqual([]);
    expect(router.url).toBe('/calendar-events');
  });

  it('keeps users on the edit route when pending-change deactivation is blocked', async () => {
    fake = new FakeAuthFacade({ authenticated: true });
    configure(fake);
    const shell = TestBed.createComponent(StubShell);
    shell.detectChanges();

    const router = TestBed.inject(Router);
    const injector = TestBed.inject(EnvironmentInjector);

    await runInInjectionContext(injector, () =>
      router.navigateByUrl('/calendar-events/event-1/edit'),
    );
    StubCalendarEventDetails.canDeactivate = false;

    const succeeded = await runInInjectionContext(injector, () =>
      router.navigateByUrl('/calendar-events'),
    );

    expect(succeeded).toBe(false);
    expect(router.url).toBe('/calendar-events/event-1/edit');
  });

  it('permits leaving the edit route when pending-change deactivation is allowed', async () => {
    fake = new FakeAuthFacade({ authenticated: true });
    configure(fake);
    const shell = TestBed.createComponent(StubShell);
    shell.detectChanges();

    const router = TestBed.inject(Router);
    const injector = TestBed.inject(EnvironmentInjector);

    await runInInjectionContext(injector, () =>
      router.navigateByUrl('/calendar-events/event-1/edit'),
    );

    const succeeded = await runInInjectionContext(injector, () =>
      router.navigateByUrl('/calendar-events'),
    );

    expect(succeeded).toBe(true);
    expect(router.url).toBe('/calendar-events');
  });
});
