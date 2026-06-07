import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';

import { AuthFacade } from './auth-facade';

export const authenticatedGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthFacade);

  if (auth.isAuthenticated()) {
    return true;
  }

  // Fire-and-forget: AuthFacade.signIn triggers a full-page redirect to
  // Entra. Awaiting it inside a CanActivate guard would block route
  // resolution while the browser navigates away, which is harmless but
  // pointless. Block route activation here; sign-in completion handling
  // is owned by the bootstrap initializer in app.config.ts (T012).
  void auth.signIn(state.url);
  return false;
};
