import { Routes } from '@angular/router';

import { CalendarEvents } from './pages/calendar-events/calendar-events';

export const routes: Routes = [
  {
    path: '',
    component: CalendarEvents,
    pathMatch: 'full',
  },
  {
    path: 'calendar-events',
    component: CalendarEvents,
  },
  {
    path: '**',
    redirectTo: '',
  },
];
