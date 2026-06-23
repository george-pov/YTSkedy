import { Routes } from '@angular/router';

import { AppLayout } from './layout/app-layout/app-layout';
import { CalendarEvents } from './pages/calendar-events/calendar-events';
import { ComponentLab } from './pages/component-lab/component-lab';
import { Home } from './pages/home/home';
import { SignedOut } from './pages/signed-out/signed-out';
import { authenticatedGuard } from './shared/auth/authenticated-guard';
import { redirectAuthenticatedGuard } from './shared/auth/redirect-authenticated-guard';
import { CalendarEventDetails } from './pages/calendar-event-details/calendar-event-details';
import { Templates } from './pages/templates/templates';
import { Platforms } from './pages/platforms/platforms';
import { Settings } from './pages/settings/settings';

export const routes: Routes = [
  {
    path: '',
    component: AppLayout,
    children: [
      {
        path: '',
        component: Home,
        pathMatch: 'full',
        canActivate: [redirectAuthenticatedGuard],
      },
      {
        path: 'calendar-events',
        component: CalendarEvents,
        canActivate: [authenticatedGuard]        
      },
      {
        path: 'calendar-events/new',
        component: CalendarEventDetails,
        canActivate: [authenticatedGuard]
      },
      {
        path: 'calendar-events/:calendarEventId/edit',
        component: CalendarEventDetails,
        canActivate: [authenticatedGuard]
      },
      {
        path: 'templates',
        component: Templates,
        canActivate: [authenticatedGuard],
      },
      {
        path: 'platforms',
        component: Platforms,
        canActivate: [authenticatedGuard],
      },
      {
        path: 'settings',
        component: Settings,
        canActivate: [authenticatedGuard],
      },
      {
        path: 'signed-out',
        component: SignedOut,
        canActivate: [redirectAuthenticatedGuard],
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
