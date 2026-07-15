import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, throwError } from 'rxjs';

import { APP_CONFIG } from 'src/app/shared/config/app-config';
import type { PlatformType } from 'src/app/shared/platforms/platform-types';
import { platformByIdUrl, platformsUrl, wordpressCategoriesUrl } from './platforms-endpoint';

export type { PlatformType } from 'src/app/shared/platforms/platform-types';

/** YouTube broadcast visibility. Mirrors the YouTube Data API privacy values. */
export type YouTubePrivacyStatus = 'private' | 'public' | 'unlisted';

/** Secret-bearing request shape and redacted response shape for YouTube OAuth. */
export interface YouTubeCredentials {
  clientId: string;
  clientSecret?: string;
  refreshToken?: string;
  clientSecretConfigured?: boolean;
  refreshTokenConfigured?: boolean;
  clientSecretDisplayValue?: string;
  refreshTokenDisplayValue?: string;
}

/** Publish settings specific to a YouTube platform. */
export interface YouTubePublishSettings {
  credentials: YouTubeCredentials;
  privacyStatus: YouTubePrivacyStatus;
  selfDeclaredMadeForKids: boolean;
  categoryId?: string | null;
  containsSyntheticMedia?: boolean;
}

export type WordPressPostStatus = 'draft' | 'pending' | 'private' | 'future' | 'publish';

/** Publish settings returned for a WordPress platform. */
export interface WordPressPublishSettings {
  siteUrl: string;
  username: string;
  postStatus: WordPressPostStatus;
  categoryIds: number[];
  sticky?: boolean;
  scheduleOffsetHours?: number;
  applicationPasswordConfigured?: boolean;
  passwordDisplayValue?: string;
  applicationPassword?: string;
}

export interface WordPressCategory {
  id: number;
  name: string;
  slug: string;
}

export interface WordPressCategoryQuery {
  search?: string;
  includeIds?: readonly number[];
  page?: number;
  pageSize?: number;
}

export interface WordPressCategoryListResponse {
  items: WordPressCategory[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

export type PlatformPublishSettings = YouTubePublishSettings | WordPressPublishSettings;

/** Platform-owned title and description template selection. */
export interface PublishingContent {
  titleTemplateId: string;
  descriptionTemplateId: string;
}

/**
 * A configured publishing platform. Secret-bearing settings are redacted in API
 * responses: YouTube returns configured flags and backend-computed display
 * values for secret values, and WordPress returns `applicationPasswordConfigured`
 * plus a backend-computed display value.
 */
export interface Platform {
  id: string;
  name: string;
  referenceKey: string | null;
  type: PlatformType;
  publishSettings?: PlatformPublishSettings;
  publishingContent: PublishingContent;
}

export interface PlatformListResponse {
  platforms: Platform[];
}

export interface CreatePlatformRequest {
  name: string;
  referenceKey?: string | null;
  type: PlatformType;
  publishSettings?: PlatformPublishSettings;
  publishingContent: PublishingContent;
}

export type CreatePlatformResponse = Platform;

/** The type is immutable, so only the name and settings travel in an update. */
export interface UpdatePlatformRequest {
  name: string;
  referenceKey?: string | null;
  publishSettings?: PlatformPublishSettings;
  publishingContent: PublishingContent;
}

export type UpdatePlatformResponse = Platform;

/** Raised when a create or update would duplicate an existing platform name. */
export class PlatformNameConflictError extends Error {
  constructor() {
    super('A platform with this name already exists.');
    this.name = 'PlatformNameConflictError';
  }
}

/** Raised when a create or update would duplicate an existing reference key. */
export class PlatformReferenceKeyConflictError extends Error {
  constructor() {
    super('A platform with this reference key already exists.');
    this.name = 'PlatformReferenceKeyConflictError';
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

  listWordPressCategories(
    platformId: string,
    query: WordPressCategoryQuery,
  ): Observable<WordPressCategoryListResponse> {
    let params = new HttpParams();
    if (query.search !== undefined) {
      params = params.set('search', query.search);
    }
    if (query.includeIds !== undefined) {
      params = params.set('includeIds', query.includeIds.join(','));
    }
    if (query.page !== undefined) {
      params = params.set('page', query.page);
    }
    if (query.pageSize !== undefined) {
      params = params.set('pageSize', query.pageSize);
    }

    return this.http.get<WordPressCategoryListResponse>(
      wordpressCategoriesUrl(this.appConfig.api, platformId),
      { params },
    );
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
  referenceKey?: string | null;
  type: PlatformType;
  publishSettings?: PlatformPublishSettings;
  publishingContent: PublishingContent;
}

function toPlatform(platform: ApiPlatform): Platform {
  return {
    id: platform.platformId,
    name: platform.name,
    referenceKey: platform.referenceKey ?? null,
    type: platform.type,
    publishSettings: copyPublishSettings(platform.publishSettings),
    publishingContent: { ...platform.publishingContent },
  };
}

function copyPublishSettings(
  settings: PlatformPublishSettings | undefined,
): PlatformPublishSettings | undefined {
  if (settings === undefined) {
    return undefined;
  }

  if ('siteUrl' in settings) {
    return { ...settings, categoryIds: [...settings.categoryIds] };
  }

  return {
    ...settings,
    credentials: { ...settings.credentials },
    categoryId: settings.categoryId ?? null,
    containsSyntheticMedia: settings.containsSyntheticMedia ?? false,
  };
}

function mapCreateError(error: unknown): Observable<never> {
  if (error instanceof HttpErrorResponse && error.status === 409) {
    if (hasConflictBodyText(error, 'reference key')) {
      return throwError(() => new PlatformReferenceKeyConflictError());
    }

    return throwError(() => new PlatformNameConflictError());
  }

  return throwError(() => error);
}

function mapUpdateError(error: unknown): Observable<never> {
  if (error instanceof HttpErrorResponse && error.status === 409) {
    if (hasConflictBodyText(error, 'reference key')) {
      return throwError(() => new PlatformReferenceKeyConflictError());
    }

    if (!hasConflictBodyText(error, 'already exists')) {
      return throwError(() => error);
    }

    return throwError(() => new PlatformNameConflictError());
  }

  return throwError(() => error);
}

function hasConflictBodyText(error: HttpErrorResponse, expected: string): boolean {
  return (
    typeof error.error === 'string' &&
    error.error.toLocaleLowerCase().includes(expected.toLocaleLowerCase())
  );
}
