import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, throwError } from 'rxjs';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import { platformByIdUrl, platformsUrl } from './platforms-endpoint';

/**
 * The external system a platform publishes to. The set is expected to grow
 * (WordPress settings are not modeled yet), but the type is treated as
 * immutable after create because it drives which settings a platform carries.
 */
export type PlatformType = 'YouTube' | 'WordPress';

/** YouTube broadcast visibility. Mirrors the YouTube Data API privacy values. */
export type YouTubePrivacyStatus = 'private' | 'public' | 'unlisted';

/** Publish settings specific to a YouTube platform. */
export interface YouTubePublishSettings {
  credentials: string;
  privacyStatus: YouTubePrivacyStatus;
  selfDeclaredMadeForKids: boolean;
}

/**
 * A configured publishing platform. `publishSettings` is present for YouTube
 * platforms; other types do not have modeled settings yet.
 */
export interface Platform {
  id: string;
  name: string;
  type: PlatformType;
  publishSettings?: YouTubePublishSettings;
}

export interface PlatformListResponse {
  platforms: Platform[];
}

export interface CreatePlatformRequest {
  name: string;
  type: PlatformType;
  publishSettings?: YouTubePublishSettings;
}

export interface CreatePlatformResponse {
  id: string;
  name: string;
  type: PlatformType;
  publishSettings?: YouTubePublishSettings;
}

/** The type is immutable, so only the name and settings travel in an update. */
export interface UpdatePlatformRequest {
  name: string;
  publishSettings?: YouTubePublishSettings;
}

export interface UpdatePlatformResponse {
  id: string;
  name: string;
  type: PlatformType;
  publishSettings?: YouTubePublishSettings;
}

/** Raised when a create or update would duplicate an existing platform name. */
export class PlatformNameConflictError extends Error {
  constructor() {
    super('A platform with this name already exists.');
    this.name = 'PlatformNameConflictError';
  }
}

@Injectable({
  providedIn: 'root',
})
export class PlatformsService {
  private readonly http = inject(HttpClient);
  private readonly appConfig = inject(APP_CONFIG);

  list(type?: PlatformType): Observable<PlatformListResponse> {
    const options = type === undefined ? {} : { params: new HttpParams().set('type', type) };

    return this.http
      .get<ApiPlatformListResponse>(platformsUrl(this.appConfig.api), options)
      .pipe(map((response) => ({ platforms: response.items.map(toPlatform) })));
  }

  create(request: CreatePlatformRequest): Observable<CreatePlatformResponse> {
    return this.http
      .post<ApiPlatform>(platformsUrl(this.appConfig.api), request)
      .pipe(map(toPlatform), catchError(mapCreateError));
  }

  update(
    _type: PlatformType,
    id: string,
    request: UpdatePlatformRequest,
  ): Observable<UpdatePlatformResponse> {
    return this.http
      .put<ApiPlatform>(platformByIdUrl(this.appConfig.api, id), request)
      .pipe(map(toPlatform), catchError(mapUpdateError));
  }

  delete(_type: PlatformType, id: string): Observable<void> {
    return this.http.delete<void>(platformByIdUrl(this.appConfig.api, id));
  }
}

interface ApiPlatformListResponse {
  items: ApiPlatform[];
}

interface ApiPlatform {
  platformId: string;
  name: string;
  type: PlatformType;
  publishSettings?: YouTubePublishSettings;
}

function toPlatform(platform: ApiPlatform): Platform {
  return {
    id: platform.platformId,
    name: platform.name,
    type: platform.type,
    publishSettings:
      platform.publishSettings === undefined ? undefined : { ...platform.publishSettings },
  };
}

function mapCreateError(error: unknown): Observable<never> {
  if (error instanceof HttpErrorResponse && error.status === 409) {
    return throwError(() => new PlatformNameConflictError());
  }

  return throwError(() => error);
}

function mapUpdateError(error: unknown): Observable<never> {
  if (
    error instanceof HttpErrorResponse &&
    error.status === 409 &&
    typeof error.error === 'string' &&
    error.error.includes('already exists')
  ) {
    return throwError(() => new PlatformNameConflictError());
  }

  return throwError(() => error);
}
