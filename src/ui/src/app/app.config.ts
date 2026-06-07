import {
  ApplicationConfig,
  EnvironmentInjector,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  runInInjectionContext,
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  IPublicClientApplication,
  LogLevel,
  PublicClientApplication,
} from '@azure/msal-browser';
import {
  MSAL_INSTANCE,
  MsalBroadcastService,
  MsalService,
} from '@azure/msal-angular';

import { routes } from './app.routes';
import { AuthFacade, MsalAuthFacade } from './shared/auth/auth-facade';
import { bearerTokenInterceptor } from './shared/auth/bearer-token-interceptor';
import { APP_CONFIG, AppConfig } from './shared/config/app-config';
import { AppConfigLoader } from './shared/config/app-config-loader';

export function msalInstanceFactory(
  config: AppConfig,
): IPublicClientApplication {
  return new PublicClientApplication({
    auth: {
      clientId: config.auth.clientId,
      authority: config.auth.authority,
      knownAuthorities: config.auth.knownAuthorities,
      redirectUri: config.auth.redirectUri,
      postLogoutRedirectUri: config.auth.postLogoutRedirectUri,
    },
    cache: {
      cacheLocation: 'sessionStorage',
    },
    system: {
      loggerOptions: {
        piiLoggingEnabled: false,
        logLevel: LogLevel.Warning,
        loggerCallback: () => {
          // T014/Decision: do not forward MSAL log messages anywhere.
          // piiLoggingEnabled + LogLevel.Warning already gate PII and verbose
          // output, but an explicit no-op callback prevents future MSAL
          // versions from accidentally writing to console.* and removes the
          // need to audit every level on upgrade.
        },
      },
    },
  });
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([bearerTokenInterceptor])),
    {
      provide: APP_CONFIG,
      useFactory: () => inject(AppConfigLoader).getConfig(),
    },
    {
      provide: MSAL_INSTANCE,
      useFactory: msalInstanceFactory,
      deps: [APP_CONFIG],
    },
    MsalService,
    MsalBroadcastService,
    { provide: AuthFacade, useClass: MsalAuthFacade },
    provideAppInitializer(async () => {
      // Bootstrap ordering:
      //   1. Capture the injector + load runtime config synchronously
      //      (inject() only works while the initializer is on the
      //      synchronous call stack; an awaited microtask exits the
      //      injection context and triggers NG0203).
      //   2. After config has loaded, resolve MsalService inside
      //      runInInjectionContext so the MSAL_INSTANCE factory runs
      //      now (with APP_CONFIG already populated) rather than
      //      eagerly during initializer setup.
      //   3. Run MSAL initialize + redirect completion + active-account
      //      selection.
      // Collapsing these into a single initializer avoids the
      // parallel-initializer race that produced
      // "Runtime config has not been loaded." at bootstrap.
      const injector = inject(EnvironmentInjector);
      const loader = inject(AppConfigLoader);

      await loader.load();

      const msal = runInInjectionContext(injector, () =>
        inject(MsalService),
      );

      await firstValueFrom(msal.initialize());
      const redirectResult = await firstValueFrom(
        msal.handleRedirectObservable(),
      );

      const instance = msal.instance;
      if (instance.getActiveAccount() === null) {
        const [firstAccount] = instance.getAllAccounts();
        if (firstAccount !== undefined) {
          instance.setActiveAccount(firstAccount);
        }
      }

      // If we just returned from an Entra redirect that carried a state
      // payload (a returnUrl set by AuthFacade.signIn or the
      // authenticatedGuard), navigate there now. Without this, the
      // user lands on the redirectUri (`/`) and the home page bounces
      // them to /calendar-events instead of restoring their original
      // intent (e.g. a bookmarked deep link).
      const returnUrl = redirectResult?.state;
      if (typeof returnUrl === 'string' && isSafeInternalUrl(returnUrl)) {
        const router = runInInjectionContext(injector, () => inject(Router));
        await router.navigateByUrl(returnUrl);
      }
    }),
  ],
};

// Only accept same-origin absolute paths. Protocol-relative URLs
// (`//evil.example`) and backslash variants (`/\\evil.example`, which some
// URL parsers treat as a scheme delimiter) both start with `/` but resolve
// to a different origin, so reject them explicitly.
function isSafeInternalUrl(candidate: string): boolean {
  if (candidate.length === 0 || !candidate.startsWith('/')) {
    return false;
  }
  if (candidate.startsWith('//') || candidate.startsWith('/\\')) {
    return false;
  }
  return true;
}


