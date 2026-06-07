import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';

import { AuthFacade } from './auth-facade';

/**
 * Bounces already-authenticated visitors away from public landing routes
 * (e.g. `/` and `/signed-out`) to `/calendar-events`. Pages used to do
 * this in `ngOnInit`; centralizing it in a guard keeps the components
 * focused on rendering and removes per-page redirect tests.
 */
export const redirectAuthenticatedGuard: CanActivateFn = (): boolean | UrlTree => {
  const auth = inject(AuthFacade);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return router.parseUrl('/calendar-events');
  }
  return true;
};
