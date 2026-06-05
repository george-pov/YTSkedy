import { Routes } from '@angular/router';

import { AppLayout } from './layout/app-layout/app-layout';
import { CalendarEvents } from './pages/calendar-events/calendar-events';
import { ComponentLab } from './pages/component-lab/component-lab';

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
      {
        path: 'component-lab',
        component: ComponentLab,
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
