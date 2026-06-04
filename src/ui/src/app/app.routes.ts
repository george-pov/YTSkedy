import { Routes } from '@angular/router';

import { AppLayout } from './layout/app-layout/app-layout';
import { CalendarEvents } from './pages/calendar-events/calendar-events';

export const routes: Routes = [
  {
    path: '',
    component: AppLayout,
    children: [
      {
        path: '',
        component: CalendarEvents,
        pathMatch: 'full',
      },
      {
        path: 'calendar-events',
        component: CalendarEvents,
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
