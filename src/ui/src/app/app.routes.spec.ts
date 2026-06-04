import { describe, expect, it } from 'vitest';

import { routes } from './app.routes';
import { CalendarEvents } from './pages/calendar-events/calendar-events';

describe('routes', () => {
  it('uses calendar events as the default route', () => {
    expect(routes).toContainEqual({
      path: '',
      component: CalendarEvents,
      pathMatch: 'full',
    });
  });

  it('keeps calendar events available at its explicit route', () => {
    expect(routes).toContainEqual({
      path: 'calendar-events',
      component: CalendarEvents,
    });
  });
});
