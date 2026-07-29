import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  type OnDestroy,
  type OnInit,
  viewChild,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { type Observable } from 'rxjs';

import { CalendarEventsService } from 'src/app/shared/api/calendar-events/calendar-events-service';
import { EventTextFieldsService } from 'src/app/shared/api/settings/event-text-fields-service';
import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { ButtonLink } from 'src/app/shared/components/button-link/button-link';
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { DateField } from 'src/app/shared/components/date/date';
import { Input } from 'src/app/shared/components/input/input';
import { ProgressBar } from 'src/app/shared/components/progress-bar/progress-bar';
import { Select } from 'src/app/shared/components/select/select';
import { TimeField } from 'src/app/shared/components/time/time';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { type PendingChangesAware } from 'src/app/shared/routing/pending-changes-guard';
import { CalendarEventDetailsState } from './calendar-event-details.state';
import { CalendarEventPlatforms } from './calendar-event-platforms/calendar-event-platforms';
import { ThumbnailEditor } from './thumbnail-editor/thumbnail-editor';

@Component({
  selector: 'app-calendar-event-details',
  imports: [
    Alert,
    Button,
    ButtonLink,
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
export class CalendarEventDetails implements OnInit, OnDestroy, PendingChangesAware {
  private readonly route = inject(ActivatedRoute);
  private readonly calendarEvents = inject(CalendarEventsService);
  private readonly eventTextFields = inject(EventTextFieldsService);
  private readonly confirmation = inject(ConfirmationDialogService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly state = new CalendarEventDetailsState({
    calendarEventId: this.route.snapshot.paramMap.get('calendarEventId'),
    calendarEvents: this.calendarEvents,
    eventTextFields: this.eventTextFields,
    confirmation: this.confirmation,
    notifications: this.notifications,
    router: this.router,
    destroyRef: this.destroyRef,
  });

  private readonly saveErrorRegion = viewChild('saveErrorRegion', {
    read: ElementRef<HTMLElement>,
  });
  private readonly deleteErrorRegion = viewChild('deleteErrorRegion', {
    read: ElementRef<HTMLElement>,
  });

  constructor() {
    effect(() => {
      if (this.state.saveErrorMessage() !== null && this.saveErrorRegion()) {
        this.saveErrorRegion()!.nativeElement.focus();
      }
    });

    effect(() => {
      if (this.state.deleteErrorMessage() !== null && this.deleteErrorRegion()) {
        this.deleteErrorRegion()!.nativeElement.focus();
      }
    });
  }

  ngOnInit(): void {
    this.state.initialize();
  }

  ngOnDestroy(): void {
    this.state.destroy();
  }

  canDeactivateWithPendingChanges(): boolean | Observable<boolean> {
    return this.state.canDeactivateWithPendingChanges();
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    this.state.submit();
  }
}
