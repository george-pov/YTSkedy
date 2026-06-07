import { InjectionToken } from '@angular/core';

export interface AppConfig {
  api: ApiConfig;
  auth: AuthConfig;
}

export interface ApiConfig {
  baseUrl: string;
}

export interface AuthConfig {
  clientId: string;
  authority: string;
  knownAuthorities: string[];
  redirectUri: string;
  postLogoutRedirectUri: string;
  calendarEventsReadScope: string;
  calendarEventsWriteScope: string;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');
