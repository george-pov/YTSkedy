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

@Component({ selector: 'spec-stub-templates', template: '' })
class StubTemplates implements PendingChangesAware {
  static canDeactivate = true;

  canDeactivateWithPendingChanges(): boolean {
    return StubTemplates.canDeactivate;
  }
}

@Component({ selector: 'spec-stub-platforms', template: '' })
class StubPlatforms implements PendingChangesAware {
  static canDeactivate = true;

  canDeactivateWithPendingChanges(): boolean {
    return StubPlatforms.canDeactivate;
  }
}

@Component({ selector: 'spec-stub-settings', template: '' })
class StubSettings implements PendingChangesAware {
  static canDeactivate = true;

  canDeactivateWithPendingChanges(): boolean {
    return StubSettings.canDeactivate;
  }
}

@Component({
  selector: 'spec-stub-shell',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
class StubShell {}

const guardedEditorRouteCases = [
  {
    label: 'calendar event create',
    url: '/calendar-events/new',
    component: StubCalendarEventDetails,
  },
  {
    label: 'calendar event edit',
    url: '/calendar-events/event-1/edit',
    component: StubCalendarEventDetails,
  },
  { label: 'templates', url: '/templates', component: StubTemplates },
  { label: 'platforms', url: '/platforms', component: StubPlatforms },
  { label: 'settings', url: '/settings', component: StubSettings },
] as const;

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
          path: 'calendar-events/new',
          component: StubCalendarEventDetails,
          canActivate: [authenticatedGuard],
          canDeactivate: [pendingChangesGuard],
        },
        {
          path: 'calendar-events/:calendarEventId/edit',
          component: StubCalendarEventDetails,
          canActivate: [authenticatedGuard],
          canDeactivate: [pendingChangesGuard],
        },
        {
          path: 'templates',
          component: StubTemplates,
          canActivate: [authenticatedGuard],
          canDeactivate: [pendingChangesGuard],
        },
        {
          path: 'platforms',
          component: StubPlatforms,
          canActivate: [authenticatedGuard],
          canDeactivate: [pendingChangesGuard],
        },
        {
          path: 'settings',
          component: StubSettings,
          canActivate: [authenticatedGuard],
          canDeactivate: [pendingChangesGuard],
        },
      ]),
      { provide: AuthFacade, useValue: fake },
    ],
  });
}

describe('protected editor routes', () => {
  let fake: FakeAuthFacade;

  beforeEach(() => {
    fake = new FakeAuthFacade();
    StubCalendarEventDetails.canDeactivate = true;
    StubTemplates.canDeactivate = true;
    StubPlatforms.canDeactivate = true;
    StubSettings.canDeactivate = true;
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

  it.each(guardedEditorRouteCases)(
    'keeps users on the $label route when pending-change deactivation is blocked',
    async ({ url, component }) => {
      fake = new FakeAuthFacade({ authenticated: true });
      configure(fake);
      const shell = TestBed.createComponent(StubShell);
      shell.detectChanges();

      const router = TestBed.inject(Router);
      const injector = TestBed.inject(EnvironmentInjector);

      await runInInjectionContext(injector, () => router.navigateByUrl(url));
      component.canDeactivate = false;

      const succeeded = await runInInjectionContext(injector, () =>
        router.navigateByUrl('/calendar-events'),
      );

      expect(succeeded).toBe(false);
      expect(router.url).toBe(url);
    },
  );

  it.each(guardedEditorRouteCases)(
    'permits leaving the $label route when pending-change deactivation is allowed',
    async ({ url }) => {
      fake = new FakeAuthFacade({ authenticated: true });
      configure(fake);
      const shell = TestBed.createComponent(StubShell);
      shell.detectChanges();

      const router = TestBed.inject(Router);
      const injector = TestBed.inject(EnvironmentInjector);

      await runInInjectionContext(injector, () => router.navigateByUrl(url));

      const succeeded = await runInInjectionContext(injector, () =>
        router.navigateByUrl('/calendar-events'),
      );

      expect(succeeded).toBe(true);
      expect(router.url).toBe('/calendar-events');
    },
  );
});
