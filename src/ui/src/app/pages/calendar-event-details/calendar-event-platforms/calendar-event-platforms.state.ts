import { HttpErrorResponse } from '@angular/common/http';
import { computed, signal, type DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Observable, switchMap, tap } from 'rxjs';

import {
  CalendarEventsService,
  type CalendarEventDetailsResponse,
  type CalendarEventPlatform,
  type EventPlatformPublishingContent,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';

export interface PublishingContentPreview extends EventPlatformPublishingContent {
  platformId: string;
  platformName: string;
}

export class CalendarEventPlatformsState {
  private readonly _platforms = signal<CalendarEventPlatform[]>([]);
  private readonly _publishingPlatformId = signal<string | null>(null);
  private readonly _publishErrorMessage = signal<string | null>(null);
  private readonly _deletingPublicationPlatformId = signal<string | null>(null);
  private readonly _deletePublicationErrorMessage = signal<string | null>(null);
  private readonly _platformActionBlockedMessage = signal<string | null>(null);
  private readonly _previewingPlatformId = signal<string | null>(null);
  private readonly _previewErrorMessage = signal<string | null>(null);
  private readonly _previewedPublishingContent = signal<PublishingContentPreview | null>(null);

  readonly platforms = this._platforms.asReadonly();
  readonly publishingPlatformId = this._publishingPlatformId.asReadonly();
  readonly publishErrorMessage = this._publishErrorMessage.asReadonly();
  readonly deletingPublicationPlatformId = this._deletingPublicationPlatformId.asReadonly();
  readonly deletePublicationErrorMessage = this._deletePublicationErrorMessage.asReadonly();
  readonly platformActionBlockedMessage = this._platformActionBlockedMessage.asReadonly();
  readonly previewingPlatformId = this._previewingPlatformId.asReadonly();
  readonly previewErrorMessage = this._previewErrorMessage.asReadonly();
  readonly previewedPublishingContent = this._previewedPublishingContent.asReadonly();
  readonly hasActiveMutation = computed(
    () =>
      this._publishingPlatformId() !== null ||
      this._deletingPublicationPlatformId() !== null ||
      this._previewingPlatformId() !== null,
  );
  readonly showStoredValuesPreviewNote = computed(() => this.hasPendingEventChanges());

  constructor(
    private readonly calendarEventsService: CalendarEventsService,
    private readonly confirmation: ConfirmationDialogService,
    private readonly notifications: NotificationService,
    private readonly calendarEventId: string | null,
    private readonly destroyRef: DestroyRef,
    private readonly hasActivePageMutation: () => boolean,
    private readonly hasPendingEventChanges: () => boolean,
    private readonly applyRefreshedEventDetails:
      (event: CalendarEventDetailsResponse) => void,
  ) {}

  applyEventDetails(event: Pick<CalendarEventDetailsResponse, 'platforms'>): void {
    this._platforms.set(event.platforms);
  }

  resetAfterLoadFailure(): void {
    this._platforms.set([]);
    this._publishingPlatformId.set(null);
    this._publishErrorMessage.set(null);
    this._deletingPublicationPlatformId.set(null);
    this._deletePublicationErrorMessage.set(null);
    this._platformActionBlockedMessage.set(null);
    this._previewingPlatformId.set(null);
    this._previewErrorMessage.set(null);
    this._previewedPublishingContent.set(null);
  }

  publishPlatform(platform: CalendarEventPlatform): void {
    if (
      this.calendarEventId === null ||
      !platform.canPublish ||
      this.hasActivePageMutation() ||
      this.hasActiveMutation()
    ) {
      return;
    }

    if (this.blockPlatformActionWhenEventChangesPending()) {
      return;
    }

    this._platformActionBlockedMessage.set(null);
    this._publishErrorMessage.set(null);
    this._publishingPlatformId.set(platform.platformId);

    this.calendarEventsService
      .publishPlatform(this.calendarEventId, platform.platformId)
      .pipe(
        switchMap((response) =>
          this.refreshEventDetailsAfterPlatformMutation(response.platformId),
        ),
        finalize(() => this._publishingPlatformId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.notifications.showSuccess('Calendar event published.');
        },
        error: (error: unknown) => {
          this._publishErrorMessage.set(describePublishError(error));
        },
      });
  }

  deletePlatformPublication(platform: CalendarEventPlatform): void {
    if (
      this.calendarEventId === null ||
      !platform.canDeletePublication ||
      this.hasActivePageMutation() ||
      this.hasActiveMutation()
    ) {
      return;
    }

    if (this.blockPlatformActionWhenEventChangesPending()) {
      return;
    }

    this._platformActionBlockedMessage.set(null);
    this.confirmation
      .confirm<'cancel' | 'delete'>({
        kind: 'warning',
        title: `Delete publication for ${platform.platformName}?`,
        body: 'This removes the provider publication and clears this platform row so it can be published again.',
        actions: [
          { id: 'cancel', label: 'Cancel' },
          { id: 'delete', label: 'Delete publication', primary: true },
        ],
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (
          result !== 'delete' ||
          this.hasActivePageMutation() ||
          this.hasActiveMutation()
        ) {
          return;
        }

        this._publishErrorMessage.set(null);
        this._deletePublicationErrorMessage.set(null);
        this._deletingPublicationPlatformId.set(platform.platformId);

        this.calendarEventsService
          .deletePlatformPublication(this.calendarEventId!, platform.platformId)
          .pipe(
            switchMap((response) =>
              this.refreshEventDetailsAfterPlatformMutation(response.platformId),
            ),
            finalize(() => this._deletingPublicationPlatformId.set(null)),
            takeUntilDestroyed(this.destroyRef),
          )
          .subscribe({
            next: () => {
              this.notifications.showSuccess('Platform publication deleted.');
            },
            error: (error: unknown) => {
              this._deletePublicationErrorMessage.set(
                describeDeletePublicationError(error),
              );
            },
          });
      });
  }

  previewPublishingContent(platform: CalendarEventPlatform): void {
    if (
      this.calendarEventId === null ||
      !platform.canPreviewPublishingContent ||
      this.hasActivePageMutation() ||
      this.hasActiveMutation()
    ) {
      return;
    }

    this._platformActionBlockedMessage.set(null);
    this._previewErrorMessage.set(null);
    this._previewedPublishingContent.set(null);
    this._previewingPlatformId.set(platform.platformId);

    this.calendarEventsService
      .getPublishingContent(this.calendarEventId, platform.platformId)
      .pipe(
        finalize(() => this._previewingPlatformId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (content) => {
          this._previewedPublishingContent.set({
            ...content,
            platformId: platform.platformId,
            platformName: platform.platformName,
          });
        },
        error: (error: unknown) => {
          this._previewErrorMessage.set(describePreviewError(error));
        },
      });
  }

  clearPreview(platformId: string): void {
    if (this._previewedPublishingContent()?.platformId === platformId) {
      this._previewedPublishingContent.set(null);
    }
  }

  private blockPlatformActionWhenEventChangesPending(): boolean {
    if (!this.hasPendingEventChanges()) {
      return false;
    }

    this._platformActionBlockedMessage.set(
      'Save or discard event changes before publishing.',
    );
    return true;
  }

  private refreshEventDetailsAfterPlatformMutation(
    platformId: string,
  ): Observable<CalendarEventDetailsResponse> {
    return this.calendarEventsService.getById(this.calendarEventId!).pipe(
      tap((event) => {
        this.applyRefreshedEventDetails(event);
        this.clearPreview(platformId);
      }),
    );
  }
}

export function describePublishError(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 403) {
    return 'You do not have permission to publish calendar events.';
  }

  if (error instanceof HttpErrorResponse && error.status === 409) {
    return 'The platform can no longer publish this event. Reload the page and try again.';
  }

  if (error instanceof HttpErrorResponse && error.status === 502) {
    return 'The platform could not publish this event. Try again later.';
  }

  return 'The platform could not publish this event. Check your connection and try again.';
}

export function describeDeletePublicationError(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 409) {
    return 'The publication can no longer be deleted. Reload the page and try again.';
  }

  if (error instanceof HttpErrorResponse && error.status === 502) {
    return 'The provider publication could not be deleted. Try again later.';
  }

  return 'The publication could not be deleted. Check your connection and try again.';
}

export function describePreviewError(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 404) {
    return 'Publishing content is no longer available. Reload the page and try again.';
  }

  if (error instanceof HttpErrorResponse && error.status === 409) {
    return 'Publishing content cannot be previewed. Reload the page and try again.';
  }

  return 'Publishing content could not be loaded. Check your connection and try again.';
}

export function thumbnailStatusText(platform: CalendarEventPlatform): string | null {
  return platform.thumbnailStatus === 'Failed'
    ? 'YouTube broadcast was created, but the thumbnail was not applied. Update it in YouTube Studio.'
    : null;
}
