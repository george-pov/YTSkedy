import { Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';

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
}

/** Raised when a create or update would duplicate an existing platform name. */
export class PlatformNameConflictError extends Error {
  constructor() {
    super('A platform with this name already exists.');
    this.name = 'PlatformNameConflictError';
  }
}

/**
 * In-memory stand-in for a future platforms API. It keeps the same
 * Observable-based contract the page will use against HTTP so swapping to a
 * real backend later is a drop-in. Data is non-durable and resets on reload;
 * this is a placeholder, not shipped persistence.
 */
@Injectable({
  providedIn: 'root',
})
export class PlatformsService {
  private sequence = 0;
  private platforms: Platform[] = this.seed();

  list(): Observable<PlatformListResponse> {
    return of({ platforms: this.platforms.map(clonePlatform) });
  }

  create(request: CreatePlatformRequest): Observable<CreatePlatformResponse> {
    const name = request.name.trim();
    if (this.isNameTaken(name, null)) {
      return throwError(() => new PlatformNameConflictError());
    }

    const created: Platform = {
      id: this.nextId(),
      name,
      type: request.type,
      publishSettings: request.publishSettings,
    };
    this.platforms = [...this.platforms, created];

    return of({ id: created.id, name: created.name, type: created.type });
  }

  update(
    type: PlatformType,
    id: string,
    request: UpdatePlatformRequest,
  ): Observable<UpdatePlatformResponse> {
    const existing = this.platforms.find(
      (platform) => platform.id === id && platform.type === type,
    );
    if (existing === undefined) {
      return throwError(() => new Error('Platform not found.'));
    }

    const name = request.name.trim();
    if (this.isNameTaken(name, id)) {
      return throwError(() => new PlatformNameConflictError());
    }

    const updated: Platform = {
      ...existing,
      name,
      publishSettings: request.publishSettings,
    };
    this.platforms = this.platforms.map((platform) =>
      platform.id === id ? updated : platform,
    );

    return of({ id: updated.id, name: updated.name, type: updated.type });
  }

  delete(type: PlatformType, id: string): Observable<void> {
    // Idempotent: removing a platform that is already gone is treated as done.
    this.platforms = this.platforms.filter(
      (platform) => !(platform.id === id && platform.type === type),
    );

    return of<void>(undefined);
  }

  private isNameTaken(name: string, exceptId: string | null): boolean {
    const normalized = name.toLowerCase();
    return this.platforms.some(
      (platform) =>
        platform.id !== exceptId && platform.name.toLowerCase() === normalized,
    );
  }

  private nextId(): string {
    this.sequence += 1;
    return `platform-${this.sequence}`;
  }

  private seed(): Platform[] {
    return [
      {
        id: this.nextId(),
        name: 'Main YouTube channel',
        type: 'YouTube',
        publishSettings: {
          credentials: 'main-youtube-channel',
          privacyStatus: 'private',
          selfDeclaredMadeForKids: false,
        },
      },
      {
        id: this.nextId(),
        name: 'Company blog',
        type: 'WordPress',
      },
    ];
  }
}

function clonePlatform(platform: Platform): Platform {
  return {
    ...platform,
    publishSettings:
      platform.publishSettings === undefined
        ? undefined
        : { ...platform.publishSettings },
  };
}
