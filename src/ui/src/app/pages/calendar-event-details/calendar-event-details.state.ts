import { HttpErrorResponse } from '@angular/common/http';
import { computed, signal, type DestroyRef, type Signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type Router } from '@angular/router';
import { finalize, map, type Observable, of, switchMap } from 'rxjs';

import {
  type CalendarEventDetailsResponse,
  type CalendarEventsService,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { type EventTextFieldsService } from 'src/app/shared/api/settings/event-text-fields-service';
import { type ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { delayedLoading } from 'src/app/shared/components/progress-bar/delayed-loading';
import { type NotificationService } from 'src/app/shared/notifications/notification-service';
import { CalendarEventDraftState } from './calendar-event-draft.state';
import { timeZoneOptions } from 'src/app/shared/date-time/time-zone-options';
import { CalendarEventPlatformsState } from './calendar-event-platforms/calendar-event-platforms.state';
import {
  ThumbnailEditorState,
  thumbnailErrorFromNavigationState,
  thumbnailErrorNavigationStateKey,
} from './thumbnail-editor/thumbnail-editor.state';

export interface CalendarEventDetailsOptions {
  calendarEventId: string | null;
  calendarEvents: CalendarEventsService;
  eventTextFields: EventTextFieldsService;
  confirmation: ConfirmationDialogService;
  notifications: NotificationService;
  router: Router;
  destroyRef: DestroyRef;
}

interface CreateCalendarEventSubmissionResult {
  calendarEventId: string;
  thumbnailErrorMessage: string | null;
}

export class CalendarEventDetailsState {
  private readonly _canDelete = signal(false);
  private readonly _isSubmitting = signal(false);
  private readonly _saveErrorMessage = signal<string | null>(null);
  private readonly _isLoading = signal(false);
  private readonly _loadFailed = signal(false);
  private readonly _isDeleting = signal(false);
  private readonly _deleteErrorMessage = signal<string | null>(null);
  private readonly _defaultStartErrorMessage = signal<string | null>(null);
  private initialized = false;
  private readonly initialThumbnailErrorMessage: string | null;

  readonly isEditMode: boolean;
  readonly pageTitle: string;
  readonly pageDescription: string;
  readonly timeZoneOptions = timeZoneOptions;
  readonly draft: CalendarEventDraftState;
  readonly thumbnailEditor: ThumbnailEditorState;
  readonly platformsState: CalendarEventPlatformsState;
  readonly canDelete = this._canDelete.asReadonly();
  readonly isSubmitting = this._isSubmitting.asReadonly();
  readonly saveErrorMessage = this._saveErrorMessage.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly showLoading = delayedLoading(() => this._isLoading());
  readonly loadFailed = this._loadFailed.asReadonly();
  readonly isDeleting = this._isDeleting.asReadonly();
  readonly deleteErrorMessage = this._deleteErrorMessage.asReadonly();
  readonly defaultStartErrorMessage = this._defaultStartErrorMessage.asReadonly();
  readonly showLockedEventAlert: Signal<boolean>;
  readonly hasActiveMutation: Signal<boolean>;
  readonly hasPendingChanges: Signal<boolean>;
  readonly deleteDisabled: Signal<boolean>;
  readonly cancelDisabled: Signal<boolean>;
  readonly saveDisabled: Signal<boolean>;

  private readonly hasActivePageMutation: Signal<boolean>;

  constructor(private readonly options: CalendarEventDetailsOptions) {
    this.isEditMode = options.calendarEventId !== null;
    this.pageTitle = this.isEditMode ? 'Edit Calendar Event' : 'Add Calendar Event';
    this.pageDescription = this.isEditMode
      ? 'Update this stream, scheduled start, and text fields.'
      : 'Schedule a new stream and its text fields.';
    this.initialThumbnailErrorMessage = thumbnailErrorFromNavigationState(
      options.router.getCurrentNavigation()?.extras.state,
    );
    this.draft = new CalendarEventDraftState(this.isEditMode);
    this.thumbnailEditor = new ThumbnailEditorState(
      options.calendarEvents,
      options.notifications,
      options.calendarEventId,
      this.isEditMode,
      options.destroyRef,
      (): boolean => this.hasActiveMutation(),
    );
    this.hasPendingChanges = computed(
      () => this.draft.hasPendingChanges() || this.thumbnailEditor.hasPendingCreateThumbnail(),
    );
    this.platformsState = new CalendarEventPlatformsState(
      options.calendarEvents,
      options.confirmation,
      options.notifications,
      options.calendarEventId,
      options.destroyRef,
      (): boolean => this.hasActivePageMutation(),
      (): boolean => this.hasPendingChanges(),
      (event): void => this.applyEventDetails(event),
    );
    this.hasActivePageMutation = computed(
      (): boolean =>
        this._isSubmitting() ||
        this._isDeleting() ||
        this.thumbnailEditor.isUploading() ||
        this.thumbnailEditor.isDeleting(),
    );
    this.hasActiveMutation = computed(
      (): boolean => this.hasActivePageMutation() || this.platformsState.hasActiveMutation(),
    );
    this.showLockedEventAlert = computed(
      () => this.isEditMode && (!this.draft.canUpdate() || !this._canDelete()),
    );
    this.deleteDisabled = computed(() => !this._canDelete() || this.hasActiveMutation());
    this.cancelDisabled = computed(() => this.hasActiveMutation() || !this.hasPendingChanges());
    this.saveDisabled = computed(
      () =>
        this.hasActiveMutation() ||
        (this.isEditMode && (!this.draft.canUpdate() || !this.draft.hasPendingChanges())),
    );
  }

  initialize(): void {
    if (this.initialized) {
      return;
    }

    this.initialized = true;
    if (this.options.calendarEventId === null) {
      this.loadCurrentFields();
      this.loadDefaultStart();
      return;
    }

    this.loadEvent(this.options.calendarEventId);
  }

  submit(): void {
    if (
      this.hasActiveMutation() ||
      (this.isEditMode && !this.draft.canUpdate()) ||
      (this.isEditMode && !this.draft.hasPendingChanges())
    ) {
      return;
    }

    this._saveErrorMessage.set(null);
    if (!this.draft.validate()) {
      return;
    }

    this._isSubmitting.set(true);
    if (this.options.calendarEventId === null) {
      this.createEvent();
      return;
    }

    this.updateEvent(this.options.calendarEventId);
  }

  cancel(): void {
    if (this.cancelDisabled()) {
      return;
    }

    this.confirmDiscardEventChanges()
      .pipe(takeUntilDestroyed(this.options.destroyRef))
      .subscribe((discard) => {
        if (discard) {
          this.draft.resetToBaseline();
          if (this.thumbnailEditor.hasPendingCreateThumbnail()) {
            this.thumbnailEditor.clearSelectedThumbnail();
          }
          this._saveErrorMessage.set(null);
        }
      });
  }

  deleteEvent(): void {
    if (this.hasActiveMutation() || !this._canDelete()) {
      return;
    }

    const deleteConfirmed = this.hasPendingChanges()
      ? this.confirmDiscardEventChanges().pipe(
          switchMap((discard) => (discard ? this.confirmDeleteEvent() : of(false))),
        )
      : this.confirmDeleteEvent();

    deleteConfirmed.pipe(takeUntilDestroyed(this.options.destroyRef)).subscribe((confirmed) => {
      if (confirmed) {
        this.deleteEventAfterConfirmation();
      }
    });
  }

  canDeactivateWithPendingChanges(): boolean | Observable<boolean> {
    if (this.hasActiveMutation()) {
      return false;
    }

    if (!this.hasPendingChanges()) {
      return true;
    }

    return this.confirmDiscardEventChanges();
  }

  destroy(): void {
    this.thumbnailEditor.destroy();
  }

  private loadCurrentFields(): void {
    this._isLoading.set(true);
    this._loadFailed.set(false);

    this.options.eventTextFields
      .get()
      .pipe(
        finalize(() => this._isLoading.set(false)),
        takeUntilDestroyed(this.options.destroyRef),
      )
      .subscribe({
        next: (response) => this.draft.applyCurrentFields(response.fields),
        error: () => this._loadFailed.set(true),
      });
  }

  private loadDefaultStart(): void {
    this._defaultStartErrorMessage.set(null);
    const fallbackTimeZoneId = this.draft.model().start.timeZoneId || undefined;

    this.options.calendarEvents
      .getDefaultStart(fallbackTimeZoneId)
      .pipe(takeUntilDestroyed(this.options.destroyRef))
      .subscribe({
        next: (defaultStart) => this.draft.applyDefaultStart(defaultStart),
        error: () =>
          this._defaultStartErrorMessage.set(
            'New calendar event defaults could not be loaded. Enter the start manually.',
          ),
      });
  }

  private loadEvent(calendarEventId: string): void {
    this._isLoading.set(true);
    this._loadFailed.set(false);

    this.options.calendarEvents
      .getById(calendarEventId)
      .pipe(
        finalize(() => this._isLoading.set(false)),
        takeUntilDestroyed(this.options.destroyRef),
      )
      .subscribe({
        next: (event) => {
          this.applyEventDetails(event);
          if (this.initialThumbnailErrorMessage !== null) {
            this.thumbnailEditor.setError(this.initialThumbnailErrorMessage);
          }
        },
        error: () => {
          this.draft.resetAfterLoadFailure();
          this._canDelete.set(false);
          this.platformsState.resetAfterLoadFailure();
          this.thumbnailEditor.resetAfterLoadFailure();
          this._loadFailed.set(true);
        },
      });
  }

  private createEvent(): void {
    this.options.calendarEvents
      .create(this.draft.createRequest())
      .pipe(
        switchMap((response) =>
          this.thumbnailEditor.uploadAfterCreate(response.calendarEventId).pipe(
            map(
              (thumbnailErrorMessage: string | null): CreateCalendarEventSubmissionResult => ({
                calendarEventId: response.calendarEventId,
                thumbnailErrorMessage,
              }),
            ),
          ),
        ),
        finalize(() => this._isSubmitting.set(false)),
        takeUntilDestroyed(this.options.destroyRef),
      )
      .subscribe({
        next: (result) => {
          this.commitCreateState();
          this._isSubmitting.set(false);
          this.options.notifications.showSuccess('Calendar event created.');
          if (result.thumbnailErrorMessage !== null) {
            this.options.router.navigateByUrl(calendarEventEditPath(result.calendarEventId), {
              state: {
                [thumbnailErrorNavigationStateKey]: result.thumbnailErrorMessage,
              },
            });
            return;
          }

          this.options.router.navigateByUrl('/calendar-events');
        },
        error: (error: unknown) => this._saveErrorMessage.set(describeSaveError(error)),
      });
  }

  private commitCreateState(): void {
    this.draft.markSaved();
    this.thumbnailEditor.clearSelectedThumbnail();
  }

  private updateEvent(calendarEventId: string): void {
    const request = this.draft.updateRequest();

    this.options.calendarEvents
      .update(calendarEventId, request)
      .pipe(
        finalize(() => this._isSubmitting.set(false)),
        takeUntilDestroyed(this.options.destroyRef),
      )
      .subscribe({
        next: () => {
          this.draft.markSaved();
          this._saveErrorMessage.set(null);
          this.options.notifications.showSuccess('Calendar event updated.');
        },
        error: (error: unknown) => this._saveErrorMessage.set(describeSaveError(error)),
      });
  }

  private deleteEventAfterConfirmation(): void {
    if (this.hasActiveMutation() || !this._canDelete()) {
      return;
    }

    this._deleteErrorMessage.set(null);
    this._isDeleting.set(true);

    this.options.calendarEvents
      .delete(this.options.calendarEventId!)
      .pipe(
        finalize(() => this._isDeleting.set(false)),
        takeUntilDestroyed(this.options.destroyRef),
      )
      .subscribe({
        next: () => {
          this._isDeleting.set(false);
          this.draft.markSaved();
          this.options.notifications.showSuccess('Calendar event deleted.');
          this.options.router.navigateByUrl('/calendar-events');
        },
        error: (error: unknown) => this.applyDeleteError(error),
      });
  }

  private applyDeleteError(error: unknown): void {
    if (error instanceof HttpErrorResponse && error.status === 404) {
      this._isDeleting.set(false);
      this.draft.markSaved();
      this._deleteErrorMessage.set(null);
      this.options.notifications.showSuccess('Calendar event no longer exists.');
      this.options.router.navigateByUrl('/calendar-events');
      return;
    }

    if (error instanceof HttpErrorResponse && error.status === 409) {
      this._deleteErrorMessage.set('Delete platform publications before deleting this event.');
      return;
    }

    this._deleteErrorMessage.set(
      'The event could not be deleted. Check your connection and try again.',
    );
  }

  private confirmDiscardEventChanges(): Observable<boolean> {
    return this.options.confirmation
      .confirm<'keep-editing' | 'discard'>({
        kind: 'warning',
        title: 'Discard unsaved event changes?',
        body: 'Unsaved scheduled start and event text changes, plus any thumbnail selected for a new event, will be lost and cannot be recovered.',
        actions: [
          { id: 'keep-editing', label: 'Keep editing' },
          {
            id: 'discard',
            label: 'Discard changes',
            primary: true,
            intent: 'danger',
          },
        ],
      })
      .pipe(map((result) => result === 'discard'));
  }

  private confirmDeleteEvent(): Observable<boolean> {
    return this.options.confirmation
      .confirm<'cancel' | 'delete'>({
        kind: 'warning',
        title: 'Delete calendar event?',
        body: 'This removes the calendar event from YTSkedy. Published provider resources are not removed by this action.',
        actions: [
          { id: 'cancel', label: 'Cancel' },
          { id: 'delete', label: 'Delete event', primary: true },
        ],
      })
      .pipe(map((result) => result === 'delete'));
  }

  private applyEventDetails(event: CalendarEventDetailsResponse): void {
    this.draft.applyEventDetails(event);
    this._canDelete.set(event.canDelete);
    this.thumbnailEditor.applyEventDetails(event);
    this.platformsState.applyEventDetails(event);
  }
}

function describeSaveError(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 409) {
    return 'The event can no longer be updated. Reload the page and try again.';
  }

  return 'The event could not be saved. Check your connection and try again.';
}

function calendarEventEditPath(calendarEventId: string): string {
  return `/calendar-events/${encodeURIComponent(calendarEventId)}/edit`;
}
