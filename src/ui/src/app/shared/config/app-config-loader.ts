import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';

import { AppConfig } from './app-config';

const configPath = 'config/app-config.json';

@Injectable({
  providedIn: 'root',
})
export class AppConfigLoader {
  private readonly document = inject(DOCUMENT);
  private config: AppConfig | undefined;

  async load(): Promise<void> {
    const response = await fetch(new URL(configPath, this.document.baseURI));

    if (!response.ok) {
      throw new Error(`Failed to load runtime config from ${configPath}.`);
    }

    this.config = parseAppConfig(await response.json());
  }

  getConfig(): AppConfig {
    if (this.config === undefined) {
      throw new Error('Runtime config has not been loaded.');
    }

    return this.config;
  }
}

export function parseAppConfig(value: unknown): AppConfig {
  if (!isRecord(value)) {
    throw new Error('Runtime config must be a JSON object.');
  }

  const api = value['api'];
  if (!isRecord(api)) {
    throw new Error('Runtime config api must be a JSON object.');
  }

  const baseUrl = api['baseUrl'];
  if (typeof baseUrl !== 'string' || baseUrl.trim().length === 0) {
    throw new Error('Runtime config api.baseUrl must be a non-empty string.');
  }

  return {
    api: {
      baseUrl: normalizeApiBaseUrl(baseUrl),
    },
  };
}

export function normalizeApiBaseUrl(baseUrl: string): string {
  const trimmedBaseUrl = baseUrl.trim();

  return trimmedBaseUrl.endsWith('/') ? trimmedBaseUrl : `${trimmedBaseUrl}/`;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
