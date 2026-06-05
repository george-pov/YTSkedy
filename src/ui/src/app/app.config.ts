import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { APP_CONFIG } from './shared/config/app-config';
import { AppConfigLoader } from './shared/config/app-config-loader';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(),
    provideAppInitializer(() => inject(AppConfigLoader).load()),
    {
      provide: APP_CONFIG,
      useFactory: () => inject(AppConfigLoader).getConfig(),
    },
  ],
};
