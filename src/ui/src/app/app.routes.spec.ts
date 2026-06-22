import { describe, expect, it } from 'vitest';

import { routes } from './app.routes';
import { AppLayout } from './layout/app-layout/app-layout';
import { CalendarEvents } from './pages/calendar-events/calendar-events';
import { ComponentLab } from './pages/component-lab/component-lab';
import { Home } from './pages/home/home';
import { Platforms } from './pages/platforms/platforms';
import { SignedOut } from './pages/signed-out/signed-out';
import { authenticatedGuard } from './shared/auth/authenticated-guard';
import { redirectAuthenticatedGuard } from './shared/auth/redirect-authenticated-guard';

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

  it('guards the platforms page behind authentication through the layout outlet', () => {
    const layoutRoute = routes.find(({ path }) => path === '');

    expect(layoutRoute?.children).toContainEqual({
      path: 'platforms',
      component: Platforms,
      canActivate: [authenticatedGuard],
    });
  });

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
