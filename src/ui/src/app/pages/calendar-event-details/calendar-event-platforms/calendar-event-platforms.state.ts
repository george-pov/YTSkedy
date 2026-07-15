import { HttpErrorResponse } from '@angular/common/http';
import { computed, signal, type DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, finalize, Observable, switchMap, tap, throwError } from 'rxjs';

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

type PlatformMutationAction = 'publish' | 'deletePublication' | 'recoverPublication';

class CalendarEventDetailsRefreshError extends Error {
  constructor(cause: unknown) {
    super('Calendar event details refresh failed.', { cause });
    this.name = 'CalendarEventDetailsRefreshError';
  }
}

export class CalendarEventPlatformsState {
  private readonly _platforms = signal<CalendarEventPlatform[]>([]);
  private readonly _publishingPlatformId = signal<string | null>(null);
  private readonly _publishErrorMessage = signal<string | null>(null);
  private readonly _deletingPublicationPlatformId = signal<string | null>(null);
  private readonly _deletePublicationErrorMessage = signal<string | null>(null);
  private readonly _recoveringPublicationPlatformId = signal<string | null>(null);
  private readonly _recoverPublicationErrorMessage = signal<string | null>(null);
  private readonly _platformActionBlockedMessage = signal<string | null>(null);
  private readonly _previewingPlatformId = signal<string | null>(null);
  private readonly _previewErrorMessage = signal<string | null>(null);
  private readonly _previewedPublishingContent = signal<PublishingContentPreview | null>(null);

  readonly platforms = this._platforms.asReadonly();
  readonly publishingPlatformId = this._publishingPlatformId.asReadonly();
  readonly publishErrorMessage = this._publishErrorMessage.asReadonly();
  readonly deletingPublicationPlatformId = this._deletingPublicationPlatformId.asReadonly();
  readonly deletePublicationErrorMessage = this._deletePublicationErrorMessage.asReadonly();
  readonly recoveringPublicationPlatformId = this._recoveringPublicationPlatformId.asReadonly();
  readonly recoverPublicationErrorMessage = this._recoverPublicationErrorMessage.asReadonly();
  readonly platformActionBlockedMessage = this._platformActionBlockedMessage.asReadonly();
  readonly previewingPlatformId = this._previewingPlatformId.asReadonly();
  readonly previewErrorMessage = this._previewErrorMessage.asReadonly();
  readonly previewedPublishingContent = this._previewedPublishingContent.asReadonly();
  readonly hasActiveMutation = computed(
    () =>
      this._publishingPlatformId() !== null ||
      this._deletingPublicationPlatformId() !== null ||
      this._recoveringPublicationPlatformId() !== null,
  );
  readonly hasActiveRequest = computed(
    () => this.hasActiveMutation() || this._previewingPlatformId() !== null,
  );
  readonly actionsDisabled = computed(
    () => this.hasActivePageMutation() || this.hasActiveRequest(),
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
    private readonly applyRefreshedEventDetails: (event: CalendarEventDetailsResponse) => void,
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
    this._recoveringPublicationPlatformId.set(null);
    this._recoverPublicationErrorMessage.set(null);
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
      this.hasActiveRequest()
    ) {
      return;
    }

    if (this.blockPlatformActionWhenEventChangesPending('publish')) {
      return;
    }

    this._platformActionBlockedMessage.set(null);
    if (platform.status === 'Failed') {
      this.confirmation
        .confirm<'cancel' | 'publish'>({
          kind: 'warning',
          title: `Retry publication for ${platform.platformName}?`,
          body: 'Verify the event on the publishing platform and delete it if necessary before retrying.',
          actions: [
            { id: 'cancel', label: 'Cancel' },
            { id: 'publish', label: 'Retry publication', primary: true },
          ],
        })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((result) => {
          if (result === 'publish' && !this.hasActivePageMutation() && !this.hasActiveRequest()) {
            this.startPublish(platform);
          }
        });
      return;
    }

    this.startPublish(platform);
  }

  private startPublish(platform: CalendarEventPlatform): void {
    this._publishErrorMessage.set(null);
    this._publishingPlatformId.set(platform.platformId);

    this.calendarEventsService
      .publishPlatform(this.calendarEventId!, platform.platformId)
      .pipe(
        switchMap((response) => {
          this.clearPreview(response.platformId);
          return this.refreshEventDetailsAfterPlatformMutation();
        }),
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
      this.hasActiveRequest()
    ) {
      return;
    }

    if (this.blockPlatformActionWhenEventChangesPending('deletePublication')) {
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
        if (result !== 'delete' || this.hasActivePageMutation() || this.hasActiveRequest()) {
          return;
        }

        this._publishErrorMessage.set(null);
        this._deletePublicationErrorMessage.set(null);
        this._deletingPublicationPlatformId.set(platform.platformId);

        this.calendarEventsService
          .deletePlatformPublication(this.calendarEventId!, platform.platformId)
          .pipe(
            switchMap((response) => {
              this.clearPreview(response.platformId);
              return this.refreshEventDetailsAfterPlatformMutation();
            }),
            finalize(() => this._deletingPublicationPlatformId.set(null)),
            takeUntilDestroyed(this.destroyRef),
          )
          .subscribe({
            next: () => {
              this.notifications.showSuccess('Platform publication deleted.');
            },
            error: (error: unknown) => {
              this._deletePublicationErrorMessage.set(describeDeletePublicationError(error));
            },
          });
      });
  }

  recoverPlatformPublication(platform: CalendarEventPlatform): void {
    if (
      this.calendarEventId === null ||
      !platform.canRecoverPublication ||
      this.hasActivePageMutation() ||
      this.hasActiveRequest()
    ) {
      return;
    }

    if (this.blockPlatformActionWhenEventChangesPending('recoverPublication')) {
      return;
    }

    this._platformActionBlockedMessage.set(null);
    this.confirmation
      .confirm<'cancel' | 'recover'>({
        kind: 'warning',
        title: `Mark publication attempt for ${platform.platformName} as failed?`,
        body: 'This attempt stopped before YTSkedy recorded a final state. Verify the event on the publishing platform first. This marks only the local attempt as Failed and does not delete any provider resource.',
        actions: [
          { id: 'cancel', label: 'Cancel' },
          { id: 'recover', label: 'Mark as failed', primary: true },
        ],
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result !== 'recover' || this.hasActivePageMutation() || this.hasActiveRequest()) {
          return;
        }

        this._recoverPublicationErrorMessage.set(null);
        this._recoveringPublicationPlatformId.set(platform.platformId);
        this.calendarEventsService
          .recoverPlatformPublication(this.calendarEventId!, platform.platformId)
          .pipe(
            switchMap(() => {
              this.clearPreview(platform.platformId);
              return this.refreshEventDetailsAfterPlatformMutation();
            }),
            finalize(() => this._recoveringPublicationPlatformId.set(null)),
            takeUntilDestroyed(this.destroyRef),
          )
          .subscribe({
            next: () => {
              this.notifications.showSuccess('Publication attempt marked as failed.');
            },
            error: (error: unknown) => {
              this._recoverPublicationErrorMessage.set(describeRecoverPublicationError(error));
            },
          });
      });
  }

  previewPublishingContent(platform: CalendarEventPlatform): void {
    if (
      this.calendarEventId === null ||
      !platform.canPreviewPublishingContent ||
      this.hasActivePageMutation() ||
      this.hasActiveRequest()
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

  private blockPlatformActionWhenEventChangesPending(action: PlatformMutationAction): boolean {
    if (!this.hasPendingEventChanges()) {
      return false;
    }

    const message =
      action === 'publish'
        ? 'Save or discard event changes before publishing.'
        : action === 'recoverPublication'
          ? 'Save or discard event changes before recovering a publication.'
          : 'Save or discard event changes before deleting a publication.';
    this._platformActionBlockedMessage.set(message);
    return true;
  }

  private refreshEventDetailsAfterPlatformMutation(): Observable<CalendarEventDetailsResponse> {
    return this.calendarEventsService.getById(this.calendarEventId!).pipe(
      catchError((error: unknown) => throwError(() => new CalendarEventDetailsRefreshError(error))),
      tap((event) => {
        this.applyRefreshedEventDetails(event);
      }),
    );
  }
}

export function describePublishError(error: unknown): string {
  if (error instanceof CalendarEventDetailsRefreshError) {
    return 'The event was published, but the latest calendar event details could not be loaded. Reload the page.';
  }

  if (error instanceof HttpErrorResponse && error.status === 403) {
    return 'You do not have permission to publish calendar events.';
  }

  if (error instanceof HttpErrorResponse && error.status === 409) {
    return 'The platform can no longer publish this event. Reload the page and try again.';
  }

  if (error instanceof HttpErrorResponse && error.status === 502) {
    return 'The platform could not publish this event. Verify the event on the publishing platform and delete it if necessary before retrying.';
  }

  return 'The platform could not publish this event. Check your connection and try again.';
}

export function describeDeletePublicationError(error: unknown): string {
  if (error instanceof CalendarEventDetailsRefreshError) {
    return 'The publication was deleted, but the latest calendar event details could not be loaded. Reload the page.';
  }

  if (hasPublicationActionError(error, 'publication_target_mismatch')) {
    return 'YTSkedy cannot delete this publication because the platform settings no longer match the target used to create it. Restore the original platform target and try again.';
  }

  if (error instanceof HttpErrorResponse && error.status === 409) {
    return 'The publication can no longer be deleted. Reload the page and try again.';
  }

  if (error instanceof HttpErrorResponse && error.status === 502) {
    return 'The provider publication could not be deleted. Try again later.';
  }

  return 'The publication could not be deleted. Check your connection and try again.';
}

export function describeRecoverPublicationError(error: unknown): string {
  if (error instanceof CalendarEventDetailsRefreshError) {
    return 'The publication attempt was marked as failed, but the latest calendar event details could not be loaded. Reload the page.';
  }

  if (error instanceof HttpErrorResponse && error.status === 403) {
    return 'You do not have permission to recover publication attempts.';
  }

  if (error instanceof HttpErrorResponse && (error.status === 404 || error.status === 409)) {
    return 'The publication attempt can no longer be recovered. Reload the page and try again.';
  }

  return 'The publication attempt could not be recovered. Check your connection and try again.';
}

function hasPublicationActionError(error: unknown, code: string): boolean {
  if (
    !(error instanceof HttpErrorResponse) ||
    typeof error.error !== 'object' ||
    error.error === null
  ) {
    return false;
  }

  return 'code' in error.error && error.error.code === code;
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

export function platformStatusText(platform: CalendarEventPlatform): string {
  return platform.status === 'NotPublished' ? 'Not published' : platform.status;
}
