import { describe, expect, it } from 'vitest';

import { routes } from './app.routes';
import { AppLayout } from './layout/app-layout/app-layout';
import { CalendarEvents } from './pages/calendar-events/calendar-events';
import { ComponentLab } from './pages/component-lab/component-lab';

describe('routes', () => {
  it('uses the app layout as the route shell', () => {
    const layoutRoute = routes.find(({ path }) => path === '');

    expect(layoutRoute).toMatchObject({
      path: '',
      component: AppLayout,
    });
  });

  it('renders calendar events through the layout outlet by default', () => {
    const layoutRoute = routes.find(({ path }) => path === '');

    expect(layoutRoute?.children).toContainEqual({
      path: '',
      component: CalendarEvents,
      pathMatch: 'full',
    });
  });

  it('keeps calendar events available through the layout outlet at its explicit route', () => {
    const layoutRoute = routes.find(({ path }) => path === '');

    expect(layoutRoute?.children).toContainEqual({
      path: 'calendar-events',
      component: CalendarEvents,
    });
  });

  it('keeps the component lab available through a direct route', () => {
    const layoutRoute = routes.find(({ path }) => path === '');

    expect(layoutRoute?.children).toContainEqual({
      path: 'component-lab',
      component: ComponentLab,
    });
  });
});
