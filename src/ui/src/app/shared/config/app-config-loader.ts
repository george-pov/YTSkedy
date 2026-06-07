import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';

import { AppConfig, AuthConfig } from './app-config';

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

  const auth = parseAuthConfig(value['auth']);

  return {
    api: {
      baseUrl: normalizeApiBaseUrl(baseUrl),
    },
    auth,
  };
}

export function normalizeApiBaseUrl(baseUrl: string): string {
  const trimmedBaseUrl = baseUrl.trim();

  return trimmedBaseUrl.endsWith('/') ? trimmedBaseUrl : `${trimmedBaseUrl}/`;
}

export function parseAuthConfig(value: unknown): AuthConfig {
  if (!isRecord(value)) {
    throw new Error('Runtime config auth must be a JSON object.');
  }

  const clientId = requireNonEmptyString(value, 'auth.clientId');
  const authority = requireNonEmptyString(value, 'auth.authority');
  const knownAuthorities = requireNonEmptyStringArray(
    value,
    'auth.knownAuthorities',
  );
  const redirectUri = requireNonEmptyString(value, 'auth.redirectUri');
  const postLogoutRedirectUri = requireNonEmptyString(
    value,
    'auth.postLogoutRedirectUri',
  );
  const calendarEventsReadScope = requireNonEmptyString(
    value,
    'auth.calendarEventsReadScope',
  );
  const calendarEventsWriteScope = requireNonEmptyString(
    value,
    'auth.calendarEventsWriteScope',
  );

  return {
    clientId,
    authority,
    knownAuthorities,
    redirectUri,
    postLogoutRedirectUri,
    calendarEventsReadScope,
    calendarEventsWriteScope,
  };
}

function requireNonEmptyString(
  source: Record<string, unknown>,
  path: string,
): string {
  const key = path.substring(path.indexOf('.') + 1);
  const raw = source[key];

  if (typeof raw !== 'string' || raw.trim().length === 0) {
    throw new Error(`Runtime config ${path} must be a non-empty string.`);
  }

  return raw.trim();
}

function requireNonEmptyStringArray(
  source: Record<string, unknown>,
  path: string,
): string[] {
  const key = path.substring(path.indexOf('.') + 1);
  const raw = source[key];

  if (!Array.isArray(raw) || raw.length === 0) {
    throw new Error(`Runtime config ${path} must be a non-empty array.`);
  }

  return raw.map((entry, index) => {
    if (typeof entry !== 'string' || entry.trim().length === 0) {
      throw new Error(
        `Runtime config ${path}[${index}] must be a non-empty string.`,
      );
    }
    return entry.trim();
  });
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
