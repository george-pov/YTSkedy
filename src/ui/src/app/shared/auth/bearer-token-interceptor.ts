import {
  HttpErrorResponse,
  HttpInterceptorFn,
  HttpResponse,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, switchMap, tap, throwError } from 'rxjs';

import { APP_CONFIG } from 'src/app/shared/config/app-config';

import { AuthFacade } from './auth-facade';
import {
  clearRecoveryFlag,
  hasRecoveryFlag,
  setRecoveryFlag,
} from './auth-recovery';
import { getRequiredScopes } from './protected-resources';

export const bearerTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const config = inject(APP_CONFIG);
  const auth = inject(AuthFacade);
  const router = inject(Router);

  const scopes = getRequiredScopes(req.url, config.api, config.auth);
  if (scopes === null) {
    return next(req);
  }

  return from(auth.acquireApiToken(scopes)).pipe(
    switchMap((token) =>
      next(
        req.clone({
          setHeaders: { Authorization: `Bearer ${token}` },
        }),
      ),
    ),
    tap({
      next: (event) => {
        if (event instanceof HttpResponse) {
          clearRecoveryFlag();
        }
      },
    }),
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        if (!hasRecoveryFlag()) {
          setRecoveryFlag();
          // Fire-and-forget: signIn triggers a full-page redirect to Entra,
          // so there is nothing useful to await inside an HTTP pipeline.
          void auth.signIn(router.url);
        }
      }
      return throwError(() => error);
    }),
  );
};
