import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  OnDestroy,
  signal,
  type Signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { form } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, map, Observable, of, switchMap } from 'rxjs';

import {
  CalendarEventsService,
  type CalendarEventDetailsResponse,
  type UpdateCalendarEventRequest,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { type PendingChangesAware } from 'src/app/shared/routing/pending-changes-guard';
import { EventTextFieldsService } from 'src/app/shared/api/settings/event-text-fields-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { DateField } from 'src/app/shared/components/date/date';
import { Input } from 'src/app/shared/components/input/input';
import { delayedLoading } from 'src/app/shared/components/progress-bar/delayed-loading';
import { ProgressBar } from 'src/app/shared/components/progress-bar/progress-bar';
import { Select } from 'src/app/shared/components/select/select';
import { TimeField } from 'src/app/shared/components/time/time';
import {
  applyCalendarEventDetailsRules,
  CalendarEventDetailsModel,
  createCalendarEventDetailsModel,
  eventTextFieldsToModel,
  formatScheduledStartUtcIso,
  patchCalendarEventDetailsModel,
  sameUpdateCalendarEventRequest,
  scheduledStartUtcPreview,
  timeZoneOptions,
  toCreateCalendarEventRequest,
  toUpdateCalendarEventRequest,
} from './calendar-event-details.form';
import { CalendarEventPlatforms } from './calendar-event-platforms/calendar-event-platforms';
import { CalendarEventPlatformsState } from './calendar-event-platforms/calendar-event-platforms.state';
import { ThumbnailEditor } from './thumbnail-editor/thumbnail-editor';
import {
  ThumbnailEditorState,
  thumbnailErrorFromNavigationState,
  thumbnailErrorNavigationStateKey,
} from './thumbnail-editor/thumbnail-editor.state';

interface CreateCalendarEventSubmissionResult {
  calendarEventId: string;
  thumbnailErrorMessage: string | null;
}

@Component({
  selector: 'app-calendar-event-details',
  imports: [
    Alert,
    Button,
    Input,
    DateField,
    TimeField,
    Select,
    ProgressBar,
    ThumbnailEditor,
    CalendarEventPlatforms,
  ],
  templateUrl: './calendar-event-details.html',
  styleUrl: './calendar-event-details.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarEventDetails implements OnDestroy, PendingChangesAware {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly calendarEventsService = inject(CalendarEventsService);
  private readonly eventTextFieldsService = inject(EventTextFieldsService);
  private readonly confirmation = inject(ConfirmationDialogService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  // The edit route carries the calendar event id; the create route does not. A
  // non-null id puts the page in edit mode: it loads the stored event snapshot
  // and repopulates the form. Create mode loads the current text-field setting.
  private readonly editingId = this.route.snapshot.paramMap.get('calendarEventId');
  private readonly initialThumbnailErrorMessage = thumbnailErrorFromNavigationState(
    this.router.getCurrentNavigation()?.extras.state,
  );
  protected readonly isEditMode = this.editingId !== null;
  protected readonly thumbnailEditor: ThumbnailEditorState = new ThumbnailEditorState(
    this.calendarEventsService,
    this.notifications,
    this.editingId,
    this.isEditMode,
    this.destroyRef,
    (): boolean => this.hasActiveMutation(),
  );

  protected readonly model = signal<CalendarEventDetailsModel>(createCalendarEventDetailsModel());
  protected readonly canUpdate = signal(!this.isEditMode);
  protected readonly canDelete = signal(false);
  protected readonly form = form(this.model, (path) =>
    applyCalendarEventDetailsRules(path, () => this.isEditMode, () => this.canUpdate()),
  );
  protected readonly timeZoneOptions = timeZoneOptions;
  protected readonly pageTitle = computed(() =>
    this.isEditMode ? 'Edit Calendar Event' : 'Add Calendar Event',
  );
  protected readonly pageDescription = computed(() =>
    this.isEditMode
      ? 'Update this stream, scheduled start, and text fields.'
      : 'Schedule a new stream and its text fields.',
  );
  protected readonly showLockedEventAlert = computed(
    () => this.isEditMode && (!this.canUpdate() || !this.canDelete()),
  );

  protected readonly isSubmitting = signal(false);
  protected readonly saveErrorMessage = signal<string | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly showLoading = delayedLoading(() => this.isLoading());
  protected readonly loadFailed = signal(false);

  protected readonly isDeleting = signal(false);
  protected readonly deleteErrorMessage = signal<string | null>(null);
  protected readonly savedEventRequest = signal<UpdateCalendarEventRequest | null>(null);
  protected readonly hasPendingEventChanges = computed(() => {
    const saved = this.savedEventRequest();
    return (
      this.isEditMode &&
      this.canUpdate() &&
      saved !== null &&
      !sameUpdateCalendarEventRequest(toUpdateCalendarEventRequest(this.model()), saved)
    );
  });
  protected readonly platformsState = new CalendarEventPlatformsState(
    this.calendarEventsService,
    this.confirmation,
    this.notifications,
    this.editingId,
    this.destroyRef,
    (): boolean => this.hasActivePageMutation(),
    (): boolean => this.hasPendingEventChanges(),
    (event): void => this.applyEventDetails(event),
  );
  private readonly hasActivePageMutation: Signal<boolean> = computed(
    (): boolean =>
      this.isSubmitting() ||
      this.isDeleting() ||
      this.thumbnailEditor.isUploading() ||
      this.thumbnailEditor.isDeleting(),
  );
  protected readonly hasActiveMutation: Signal<boolean> = computed(
    (): boolean => this.hasActivePageMutation() || this.platformsState.hasActiveMutation(),
  );
  protected readonly deleteDisabled = computed(() => !this.canDelete() || this.hasActiveMutation());
  protected readonly cancelDisabled = computed(() => this.hasActiveMutation());
  protected readonly saveDisabled = computed(
    () =>
      this.hasActiveMutation() ||
      (this.isEditMode && (!this.canUpdate() || !this.hasPendingEventChanges())),
  );

  // Editable starts use a live UTC preview. Locked edit-mode events keep the
  // backend-provided UTC instant instead of deriving a local preview.
  private readonly loadedScheduledStartUtc = signal<string | null>(null);
  protected readonly scheduledStartUtcDisplay = computed(() => {
    if (!this.isEditMode || this.canUpdate()) {
      const start = this.model().start;
      return scheduledStartUtcPreview(start.date, start.time, start.timeZoneId);
    }

    const loaded = this.loadedScheduledStartUtc();
    return loaded === null ? '' : formatScheduledStartUtcIso(loaded);
  });

  private readonly errorRegion = viewChild('errorRegion', {
    read: ElementRef<HTMLElement>,
  });

  // True once the start group is touched and the cross-field future-start rule
  // reports its error. Rendered next to the scheduled-start section.
  protected readonly startFutureError = computed(() => {
    const start = this.form.start();
    return start.touched() && start.errors().some((error) => error.kind === 'startInPast');
  });

  constructor() {
    effect(() => {
      if (this.saveErrorMessage() !== null && this.errorRegion()) {
        this.errorRegion()!.nativeElement.focus();
      }
    });

    if (this.editingId !== null) {
      this.loadEvent(this.editingId);
    } else {
      this.loadCurrentFields();
    }
  }

  ngOnDestroy(): void {
    this.thumbnailEditor.destroy();
  }

  canDeactivateWithPendingChanges(): boolean | Observable<boolean> {
    if (!this.hasPendingEventChanges() || this.hasActiveMutation()) {
      return true;
    }

    return this.confirmDiscardEventChanges();
  }

  private loadCurrentFields(): void {
    this.isLoading.set(true);
    this.loadFailed.set(false);

    this.eventTextFieldsService
      .get()
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.model.set(
            createCalendarEventDetailsModel(
              this.model().start.timeZoneId,
              eventTextFieldsToModel(response.fields),
            ),
          );
        },
        error: () => {
          this.loadFailed.set(true);
        },
      });
  }

  private loadEvent(calendarEventId: string): void {
    this.isLoading.set(true);
    this.loadFailed.set(false);

    this.calendarEventsService
      .getById(calendarEventId)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (event) => {
          this.applyEventDetails(event);
          if (this.initialThumbnailErrorMessage !== null) {
            this.thumbnailEditor.setError(this.initialThumbnailErrorMessage);
          }
        },
        error: () => {
          this.canUpdate.set(false);
          this.canDelete.set(false);
          this.savedEventRequest.set(null);
          this.platformsState.resetAfterLoadFailure();
          this.thumbnailEditor.resetAfterLoadFailure();
          this.loadFailed.set(true);
        },
      });
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    this.submit();
  }

  protected submit(): void {
    if (this.hasActiveMutation()) {
      return;
    }

    if (this.isEditMode && !this.canUpdate()) {
      return;
    }

    if (this.isEditMode && !this.hasPendingEventChanges()) {
      return;
    }

    this.saveErrorMessage.set(null);

    if (this.form().invalid()) {
      this.form().markAsTouched();
      return;
    }

    this.isSubmitting.set(true);

    if (this.editingId === null) {
      this.calendarEventsService
        .create(toCreateCalendarEventRequest(this.model()))
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
          finalize(() => this.isSubmitting.set(false)),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe({
          next: (result) => {
            this.notifications.showSuccess('Calendar event created.');
            if (result.thumbnailErrorMessage !== null) {
              this.router.navigateByUrl(calendarEventEditPath(result.calendarEventId), {
                state: {
                  [thumbnailErrorNavigationStateKey]: result.thumbnailErrorMessage,
                },
              });
              return;
            }

            this.router.navigateByUrl('/calendar-events');
          },
          error: (error: unknown) => {
            this.saveErrorMessage.set(describeSaveError(error));
          },
        });
      return;
    }

    const request = toUpdateCalendarEventRequest(this.model());

    this.calendarEventsService
      .update(this.editingId, request)
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.savedEventRequest.set(request);
          this.saveErrorMessage.set(null);
          this.notifications.showSuccess('Calendar event updated.');
        },
        error: (error: unknown) => {
          this.saveErrorMessage.set(describeSaveError(error));
        },
      });
  }

  protected cancel(): void {
    if (this.hasActiveMutation()) {
      return;
    }

    if (!this.hasPendingEventChanges()) {
      this.router.navigateByUrl('/calendar-events');
      return;
    }

    this.confirmDiscardEventChanges()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((discard) => {
        if (discard) {
          this.router.navigateByUrl('/calendar-events');
        }
      });
  }

  protected deleteEvent(): void {
    if (
      this.hasActiveMutation() ||
      !this.canDelete()
    ) {
      return;
    }

    const deleteConfirmed = this.hasPendingEventChanges()
      ? this.confirmDiscardEventChanges().pipe(
          switchMap((discard) => (discard ? this.confirmDeleteEvent() : of(false))),
        )
      : this.confirmDeleteEvent();

    deleteConfirmed
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed) => {
        if (confirmed) {
          this.deleteEventAfterConfirmation();
        }
      });
  }

  private deleteEventAfterConfirmation(): void {
    // Page operations are mutually exclusive here so row preview, save, and row
    // mutations cannot race event delete. The backend owns final delete eligibility.
    if (
      this.hasActiveMutation() ||
      !this.canDelete()
    ) {
      return;
    }

    this.deleteErrorMessage.set(null);
    this.isDeleting.set(true);

    this.calendarEventsService
      .delete(this.editingId!)
      .pipe(
        finalize(() => this.isDeleting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.notifications.showSuccess('Calendar event deleted.');
          this.router.navigateByUrl('/calendar-events');
        },
        error: (error: unknown) => {
          if (error instanceof HttpErrorResponse && error.status === 404) {
            this.deleteErrorMessage.set(null);
            this.notifications.showSuccess('Calendar event no longer exists.');
            this.router.navigateByUrl('/calendar-events');
            return;
          }

          if (error instanceof HttpErrorResponse && error.status === 409) {
            this.deleteErrorMessage.set(
              'Delete platform publications before deleting this event.',
            );
            return;
          }

          this.deleteErrorMessage.set(
            'The event could not be deleted. Check your connection and try again.',
          );
        },
      });
  }

  private confirmDiscardEventChanges(): Observable<boolean> {
    return this.confirmation
      .confirm<'keep-editing' | 'discard'>({
        kind: 'warning',
        title: 'Discard unsaved event changes?',
        body: 'Scheduled start and event text changes have not been saved.',
        actions: [
          { id: 'keep-editing', label: 'Keep editing' },
          { id: 'discard', label: 'Discard changes', primary: true },
        ],
      })
      .pipe(map((result) => result === 'discard'));
  }

  private confirmDeleteEvent(): Observable<boolean> {
    return this.confirmation
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
    patchCalendarEventDetailsModel(this.model, event);
    this.savedEventRequest.set(toUpdateCalendarEventRequest(this.model()));
    this.loadedScheduledStartUtc.set(event.scheduledStartUtc);
    this.canUpdate.set(event.canUpdate);
    this.canDelete.set(event.canDelete);
    this.thumbnailEditor.applyEventDetails(event);
    this.platformsState.applyEventDetails(event);
  }
}

// A 409 is a backend eligibility conflict. Reloading refreshes the event state.
function describeSaveError(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 409) {
    return 'The event can no longer be updated. Reload the page and try again.';
  }

  return 'The event could not be saved. Check your connection and try again.';
}

function calendarEventEditPath(calendarEventId: string): string {
  return `/calendar-events/${encodeURIComponent(calendarEventId)}/edit`;
}
