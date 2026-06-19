import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, Observable } from 'rxjs';

import { CalendarEventsService } from 'src/app/shared/api/calendar-events/calendar-events-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { DateField } from 'src/app/shared/components/date/date';
import { Input } from 'src/app/shared/components/input/input';
import { ProgressBar } from 'src/app/shared/components/progress-bar/progress-bar';
import { Select } from 'src/app/shared/components/select/select';
import { TimeField } from 'src/app/shared/components/time/time';
import {
  createCalendarEventDetailsForm,
  formatScheduledStartUtcIso,
  patchCalendarEventDetailsForm,
  scheduledStartUtcPreview,
  timeZoneOptions,
  toCreateCalendarEventRequest,
  toUpdateCalendarEventRequest,
} from './calendar-event-details.form';

@Component({
  selector: 'app-calendar-event-details',
  imports: [ReactiveFormsModule, Alert, Button, Input, DateField, TimeField, Select, ProgressBar],
  templateUrl: './calendar-event-details.html',
  styleUrl: './calendar-event-details.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarEventDetails {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly calendarEventsService = inject(CalendarEventsService);
  private readonly notifications = inject(NotificationService);

  // The edit route carries the calendar event id; the create route does not. A
  // non-null id puts the page in edit mode: it loads the event and repopulates
  // the form. Saving an edit is not implemented yet.
  private readonly editingId = this.route.snapshot.paramMap.get('calendarEventId');
  protected readonly isEditMode = this.editingId !== null;

  protected readonly form = createCalendarEventDetailsForm();
  protected readonly timeZoneOptions = timeZoneOptions;

  protected readonly isSubmitting = signal(false);
  protected readonly saveErrorMessage = signal<string | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly loadFailed = signal(false);

  protected readonly isDeleting = signal(false);
  protected readonly deleteErrorMessage = signal<string | null>(null);

  // Action eligibility comes from the loaded event's API-computed flags; the
  // page never re-derives it from status, scheduled start, or broadcast id.
  // Delete is hidden entirely in create mode and enabled only when the loaded
  // event is deletable; Save is always available when creating and enabled when
  // editing only while the loaded event is updatable (Draft-only).
  private readonly loadedCanDelete = signal(false);
  private readonly loadedCanUpdate = signal(false);
  protected readonly canDelete = computed(() => this.isEditMode && this.loadedCanDelete());
  protected readonly canSave = computed(() => !this.isEditMode || this.loadedCanUpdate());

  // In edit mode the stored UTC instant comes from the loaded event (exact). In
  // create mode it is derived live from the start controls so the operator sees
  // how the chosen local start translates to UTC.
  private readonly loadedScheduledStartUtc = signal<string | null>(null);
  private readonly startValue = toSignal(this.form.controls.start.valueChanges, {
    initialValue: this.form.controls.start.getRawValue(),
  });
  protected readonly scheduledStartUtcDisplay = computed(() => {
    const loaded = this.loadedScheduledStartUtc();
    if (loaded !== null) {
      return formatScheduledStartUtcIso(loaded);
    }

    const start = this.startValue();
    return scheduledStartUtcPreview(start.date, start.time, start.timeZoneId);
  });

  private readonly errorRegion = viewChild('errorRegion', {
    read: ElementRef<HTMLElement>,
  });

  protected readonly startDateErrors = {
    required: 'Start date is required.',
  };
  protected readonly startTimeErrors = {
    required: 'Start time is required.',
  };
  protected readonly timeZoneErrors = {
    required: 'Time zone is required.',
  };
  protected readonly enTitleErrors = {
    required: 'English title is required.',
    maxlength: 'English title is too long.',
  };
  protected readonly enDescriptionErrors = {
    required: 'English description is required.',
    maxlength: 'English description is too long.',
  };
  protected readonly ruTitleErrors = {
    required: 'Russian title is required.',
    maxlength: 'Russian title is too long.',
  };
  protected readonly ruDescriptionErrors = {
    required: 'Russian description is required.',
    maxlength: 'Russian description is too long.',
  };

  constructor() {
    effect(() => {
      if (this.saveErrorMessage() !== null && this.errorRegion()) {
        this.errorRegion()!.nativeElement.focus();
      }
    });

    if (this.editingId !== null) {
      this.loadEvent(this.editingId);
    }
  }

  private loadEvent(calendarEventId: string): void {
    this.isLoading.set(true);
    this.loadFailed.set(false);

    this.calendarEventsService
      .getById(calendarEventId)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (event) => {
          patchCalendarEventDetailsForm(this.form, event);
          this.loadedScheduledStartUtc.set(event.scheduledStartUtc);
          this.loadedCanDelete.set(event.canDelete);
          this.loadedCanUpdate.set(event.canUpdate);
          // Descriptions-only edit: the scheduled start is the event identity
          // and cannot change, so disable those controls. Disabling also
          // excludes them from validation, so a past start does not block
          // editing the descriptions of an existing event.
          this.form.controls.start.disable();
        },
        error: () => {
          this.loadFailed.set(true);
        },
      });
  }

  protected get startHasFutureError(): boolean {
    const start = this.form.controls.start;
    return start.touched && start.hasError('startInPast');
  }

  protected submit(): void {
    if (this.isSubmitting() || this.isDeleting()) {
      return;
    }

    // Update is Draft-only: a stale client whose loaded event is no longer
    // updatable must not call the API. The backend rejects it as well.
    if (this.isEditMode && !this.loadedCanUpdate()) {
      return;
    }

    this.saveErrorMessage.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);

    // Edit updates descriptions in place; create posts a new event. Both
    // responses are ignored, so the union is typed as the wider observable.
    const request$: Observable<unknown> =
      this.editingId === null
        ? this.calendarEventsService.create(toCreateCalendarEventRequest(this.form))
        : this.calendarEventsService.update(
            this.editingId,
            toUpdateCalendarEventRequest(this.form),
          );

    request$.pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
      next: () => {
        this.notifications.showSuccess(
          this.isEditMode ? 'Calendar event updated.' : 'Calendar event created.',
        );
        this.router.navigateByUrl('/calendar-events');
      },
      error: (error: unknown) => {
        this.saveErrorMessage.set(describeSaveError(error));
      },
    });
  }

  protected cancel(): void {
    this.router.navigateByUrl('/calendar-events');
  }

  protected deleteEvent(): void {
    // Page mutations are mutually exclusive: a save in flight blocks delete and
    // an in-flight delete blocks re-entry. Only a loaded Draft in edit mode is
    // deletable, which canDelete enforces.
    if (this.isDeleting() || this.isSubmitting() || !this.canDelete()) {
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
          // 404 means the row is already gone; treat that as completed cleanup
          // and leave for the list. 409 means it is no longer deletable in its
          // current state, so keep the operator here with an explanation. 502
          // means the YouTube broadcast could not be deleted and the local row
          // was kept. Anything else is a generic transient failure.
          if (error instanceof HttpErrorResponse && error.status === 404) {
            this.deleteErrorMessage.set(null);
            this.notifications.showSuccess('Calendar event no longer exists.');
            this.router.navigateByUrl('/calendar-events');
            return;
          }

          if (error instanceof HttpErrorResponse && error.status === 409) {
            this.deleteErrorMessage.set(
              'The event can no longer be deleted. Reload the page and try again.',
            );
            return;
          }

          if (error instanceof HttpErrorResponse && error.status === 502) {
            this.deleteErrorMessage.set(
              'The YouTube broadcast could not be deleted. Try again later.',
            );
            return;
          }

          this.deleteErrorMessage.set(
            'The event could not be deleted. Check your connection and try again.',
          );
        },
      });
  }
}

// A 409 means the event is no longer updatable (it left Draft); reloading is the
// recovery. Anything else is a transient or connection failure.
function describeSaveError(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 409) {
    return 'The event can no longer be updated. Reload the page and try again.';
  }

  return 'The event could not be saved. Check your connection and try again.';
}
