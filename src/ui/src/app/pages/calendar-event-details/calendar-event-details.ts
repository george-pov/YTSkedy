import {
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';

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
  patchCalendarEventDetailsForm,
  timeZoneOptions,
  toCreateCalendarEventRequest,
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
  protected readonly submitFailed = signal(false);
  protected readonly isLoading = signal(false);
  protected readonly loadFailed = signal(false);

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
      if (this.submitFailed() && this.errorRegion()) {
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
    // Saving an edit is not implemented yet, so edit mode never creates a new
    // event. Submitting (including via Enter) is a no-op in edit mode.
    if (this.isSubmitting() || this.isEditMode) {
      return;
    }

    this.submitFailed.set(false);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);

    this.calendarEventsService
      .create(toCreateCalendarEventRequest(this.form))
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.notifications.showSuccess('Calendar event created.');
          this.router.navigateByUrl('/calendar-events');
        },
        error: () => {
          this.submitFailed.set(true);
        },
      });
  }

  protected cancel(): void {
    this.router.navigateByUrl('/calendar-events');
  }
}
