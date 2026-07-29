import { describe, expect, it } from 'vitest';

import { routes } from './app.routes';
import { AppLayout } from './layout/app-layout/app-layout';
import { CalendarEventDetails } from './pages/calendar-event-details/calendar-event-details';
import { CalendarEvents } from './pages/calendar-events/calendar-events';
import { ComponentLab } from './pages/component-lab/component-lab';
import { Home } from './pages/home/home';
import { Platforms } from './pages/platforms/platforms';
import { Settings } from './pages/settings/settings';
import { SignedOut } from './pages/signed-out/signed-out';
import { Templates } from './pages/templates/templates';
import { authenticatedGuard } from './shared/auth/authenticated-guard';
import { redirectAuthenticatedGuard } from './shared/auth/redirect-authenticated-guard';
import { pendingChangesGuard } from './shared/routing/pending-changes-guard';

const guardedEditorRouteCases = [
  {
    path: 'calendar-events/new',
    component: CalendarEventDetails,
  },
  {
    path: 'calendar-events/:calendarEventId/edit',
    component: CalendarEventDetails,
  },
  { path: 'templates', component: Templates },
  { path: 'platforms', component: Platforms },
  { path: 'settings', component: Settings },
] as const;

describe('routes', () => {
  it('uses the app layout as the route shell', () => {
    const layoutRoute = routes.find(({ path }) => path === '');

    expect(layoutRoute).toMatchObject({
      path: '',
      component: AppLayout,
    });
  });

  it('renders the public home page at the root path and bounces signed-in visitors', () => {
    const layoutRoute = routes.find(({ path }) => path === '');

    expect(layoutRoute?.children).toContainEqual({
      path: '',
      component: Home,
      pathMatch: 'full',
      canActivate: [redirectAuthenticatedGuard],
    });
  });

  it('keeps calendar events available through the layout outlet at its explicit route', () => {
    const layoutRoute = routes.find(({ path }) => path === '');

    expect(layoutRoute?.children).toContainEqual({
      path: 'calendar-events',
      component: CalendarEvents,
      canActivate: [authenticatedGuard],
    });
  });

  it.each(guardedEditorRouteCases)(
    'guards $path behind authentication and pending-change route protection',
    ({ path, component }) => {
      const layoutRoute = routes.find((route) => route.path === '');

      expect(layoutRoute?.children).toContainEqual({
        path,
        component,
        canActivate: [authenticatedGuard],
        canDeactivate: [pendingChangesGuard],
      });
    },
  );

  it('keeps the component lab available through a direct route', () => {
    const layoutRoute = routes.find(({ path }) => path === '');

    expect(layoutRoute?.children).toContainEqual({
      path: 'component-lab',
      component: ComponentLab,
    });
  });

  it('renders the signed-out confirmation page at /signed-out and bounces signed-in visitors', () => {
    const layoutRoute = routes.find(({ path }) => path === '');

    expect(layoutRoute?.children).toContainEqual({
      path: 'signed-out',
      component: SignedOut,
      canActivate: [redirectAuthenticatedGuard],
    });
  });

  it('redirects unknown URLs to the root path', () => {
    expect(routes).toContainEqual({
      path: '**',
      redirectTo: '',
    });
  });
});
