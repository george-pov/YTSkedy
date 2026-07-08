import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  OnDestroy,
  signal,
  type Signal,
  viewChild,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { form } from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, map, Observable, of, switchMap, tap } from 'rxjs';

import {
  CalendarEventsService,
  type CalendarEventDetailsResponse,
  type CalendarEventPlatform,
  type EventPlatformPublishingContent,
  type UpdateCalendarEventRequest,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { type PendingChangesAware } from 'src/app/shared/routing/pending-changes-guard';
import { EventTextFieldsService } from 'src/app/shared/api/settings/event-text-fields-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { DataTable } from 'src/app/shared/components/data-table/data-table';
import { DataTableCell } from 'src/app/shared/components/data-table/data-table-cell';
import { DataTableColumn } from 'src/app/shared/components/data-table/data-table-column';
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
import { ThumbnailEditor } from './thumbnail-editor/thumbnail-editor';
import {
  ThumbnailEditorState,
  thumbnailErrorFromNavigationState,
  thumbnailErrorNavigationStateKey,
} from './thumbnail-editor/thumbnail-editor.state';

interface PublishingContentPreview extends EventPlatformPublishingContent {
  platformId: string;
  platformName: string;
}

interface CreateCalendarEventSubmissionResult {
  calendarEventId: string;
  thumbnailErrorMessage: string | null;
}

@Component({
  selector: 'app-calendar-event-details',
  imports: [
    Alert,
    Button,
    DataTable,
    DataTableCell,
    Input,
    DateField,
    TimeField,
    Select,
    ProgressBar,
    ThumbnailEditor,
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
    (): boolean => this.hasActiveMutation(),
  );

  protected readonly model = signal<CalendarEventDetailsModel>(createCalendarEventDetailsModel());
  protected readonly canUpdate = signal(!this.isEditMode);
  protected readonly canDelete = signal(false);
  protected readonly form = form(this.model, (path) =>
    applyCalendarEventDetailsRules(path, () => this.isEditMode, () => this.canUpdate()),
  );
  protected readonly timeZoneOptions = timeZoneOptions;

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
  protected readonly platforms = signal<CalendarEventPlatform[]>([]);
  protected readonly publishingPlatformId = signal<string | null>(null);
  protected readonly publishErrorMessage = signal<string | null>(null);
  protected readonly deletingPublicationPlatformId = signal<string | null>(null);
  protected readonly deletePublicationErrorMessage = signal<string | null>(null);
  protected readonly platformActionBlockedMessage = signal<string | null>(null);
  protected readonly previewingPlatformId = signal<string | null>(null);
  protected readonly previewErrorMessage = signal<string | null>(null);
  protected readonly previewedPublishingContent = signal<PublishingContentPreview | null>(null);
  protected readonly hasActiveMutation: Signal<boolean> = computed(
    (): boolean =>
      this.isSubmitting() ||
      this.isDeleting() ||
      this.thumbnailEditor.isUploading() ||
      this.thumbnailEditor.isDeleting() ||
      this.publishingPlatformId() !== null ||
      this.deletingPublicationPlatformId() !== null ||
      this.previewingPlatformId() !== null,
  );
  protected readonly platformColumns: readonly DataTableColumn<CalendarEventPlatform>[] = [
    { key: 'type', header: 'Type', value: (platform) => platform.platformType },
    { key: 'name', header: 'Name', value: (platform) => platform.platformName, truncate: true },
    { key: 'status', header: 'Status', value: (platform) => platform.status },
    { key: 'actions', header: 'Actions' },
  ];
  protected readonly thumbnailStatusText = thumbnailStatusText;

  // Editable starts use a live UTC preview. Locked edit-mode events keep the
  // backend-provided UTC instant instead of deriving a local preview.
  private readonly loadedScheduledStartUtc = signal<string | null>(null);
  protected readonly scheduledStartUtcDisplay = computed(() => {
    const start = this.model().start;
    if (!this.isEditMode || this.canUpdate()) {
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
      .pipe(finalize(() => this.isLoading.set(false)))
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
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (event) => {
          this.applyEventDetails(event);
          this.previewErrorMessage.set(null);
          this.previewedPublishingContent.set(null);
          if (this.initialThumbnailErrorMessage !== null) {
            this.thumbnailEditor.setError(this.initialThumbnailErrorMessage);
          }
        },
        error: () => {
          this.canUpdate.set(false);
          this.canDelete.set(false);
          this.savedEventRequest.set(null);
          this.platforms.set([]);
          this.publishErrorMessage.set(null);
          this.deletePublicationErrorMessage.set(null);
          this.platformActionBlockedMessage.set(null);
          this.previewErrorMessage.set(null);
          this.previewedPublishingContent.set(null);
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
      .pipe(finalize(() => this.isSubmitting.set(false)))
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

    this.confirmDiscardEventChanges().subscribe((discard) => {
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

    deleteConfirmed.subscribe((confirmed) => {
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
      .pipe(finalize(() => this.isDeleting.set(false)))
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

  protected publishPlatform(platform: CalendarEventPlatform): void {
    if (
      this.editingId === null ||
      !platform.canPublish ||
      this.hasActiveMutation()
    ) {
      return;
    }

    if (this.blockPlatformActionWhenEventChangesPending()) {
      return;
    }

    this.platformActionBlockedMessage.set(null);
    this.publishErrorMessage.set(null);
    this.publishingPlatformId.set(platform.platformId);

    this.calendarEventsService
      .publishPlatform(this.editingId, platform.platformId)
      .pipe(
        switchMap((response) =>
          this.refreshEventDetailsAfterPlatformMutation(response.platformId),
        ),
        finalize(() => this.publishingPlatformId.set(null)),
      )
      .subscribe({
        next: () => {
          this.notifications.showSuccess('Calendar event published.');
        },
        error: (error: unknown) => {
          this.publishErrorMessage.set(describePublishError(error));
        },
      });
  }

  protected deletePlatformPublication(platform: CalendarEventPlatform): void {
    if (this.editingId === null || !platform.canDeletePublication || this.hasActiveMutation()) {
      return;
    }

    if (this.blockPlatformActionWhenEventChangesPending()) {
      return;
    }

    this.platformActionBlockedMessage.set(null);
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
      .subscribe((result) => {
        if (result !== 'delete' || this.hasActiveMutation()) {
          return;
        }

        this.publishErrorMessage.set(null);
        this.deletePublicationErrorMessage.set(null);
        this.deletingPublicationPlatformId.set(platform.platformId);

        this.calendarEventsService
          .deletePlatformPublication(this.editingId!, platform.platformId)
          .pipe(
            switchMap((response) =>
              this.refreshEventDetailsAfterPlatformMutation(response.platformId),
            ),
            finalize(() => this.deletingPublicationPlatformId.set(null)),
          )
          .subscribe({
            next: () => {
              this.notifications.showSuccess('Platform publication deleted.');
            },
            error: (error: unknown) => {
              this.deletePublicationErrorMessage.set(describeDeletePublicationError(error));
            },
          });
      });
  }

  protected previewPublishingContent(platform: CalendarEventPlatform): void {
    if (
      this.editingId === null ||
      !platform.canPreviewPublishingContent ||
      this.hasActiveMutation() ||
      this.previewingPlatformId() !== null
    ) {
      return;
    }

    this.platformActionBlockedMessage.set(null);
    this.previewErrorMessage.set(null);
    this.previewedPublishingContent.set(null);
    this.previewingPlatformId.set(platform.platformId);

    this.calendarEventsService
      .getPublishingContent(this.editingId, platform.platformId)
      .pipe(finalize(() => this.previewingPlatformId.set(null)))
      .subscribe({
        next: (content) => {
          this.previewedPublishingContent.set({
            ...content,
            platformId: platform.platformId,
            platformName: platform.platformName,
          });
        },
        error: (error: unknown) => {
          this.previewErrorMessage.set(describePreviewError(error));
        },
      });
  }

  private clearPreview(platformId: string): void {
    if (this.previewedPublishingContent()?.platformId === platformId) {
      this.previewedPublishingContent.set(null);
    }
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

  private blockPlatformActionWhenEventChangesPending(): boolean {
    if (!this.hasPendingEventChanges()) {
      return false;
    }

    this.platformActionBlockedMessage.set('Save or discard event changes before publishing.');
    return true;
  }

  private refreshEventDetailsAfterPlatformMutation(
    platformId: string,
  ): Observable<CalendarEventDetailsResponse> {
    return this.calendarEventsService.getById(this.editingId!).pipe(
      tap((event) => {
        this.applyEventDetails(event);
        this.clearPreview(platformId);
      }),
    );
  }

  private applyEventDetails(event: CalendarEventDetailsResponse): void {
    patchCalendarEventDetailsModel(this.model, event);
    this.savedEventRequest.set(toUpdateCalendarEventRequest(this.model()));
    this.loadedScheduledStartUtc.set(event.scheduledStartUtc);
    this.platforms.set(event.platforms);
    this.canUpdate.set(event.canUpdate);
    this.canDelete.set(event.canDelete);
    this.thumbnailEditor.applyEventDetails(event);
  }
}

// A 409 is a backend eligibility conflict. Reloading refreshes the event state.
function describeSaveError(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 409) {
    return 'The event can no longer be updated. Reload the page and try again.';
  }

  return 'The event could not be saved. Check your connection and try again.';
}

function describePublishError(error: unknown): string {
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

function describeDeletePublicationError(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 409) {
    return 'The publication can no longer be deleted. Reload the page and try again.';
  }

  if (error instanceof HttpErrorResponse && error.status === 502) {
    return 'The provider publication could not be deleted. Try again later.';
  }

  return 'The publication could not be deleted. Check your connection and try again.';
}

function describePreviewError(error: unknown): string {
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

function calendarEventEditPath(calendarEventId: string): string {
  return `/calendar-events/${encodeURIComponent(calendarEventId)}/edit`;
}
