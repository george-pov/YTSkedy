import { InjectionToken } from '@angular/core';

export interface AppConfig {
  api: ApiConfig;
}

export interface ApiConfig {
  baseUrl: string;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');
