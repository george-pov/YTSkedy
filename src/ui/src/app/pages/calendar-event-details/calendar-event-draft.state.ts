import { computed, signal } from '@angular/core';
import { form } from '@angular/forms/signals';

import {
  type CalendarEventDetailsResponse,
  type CalendarEventDefaultStart,
  type CreateCalendarEventRequest,
  type UpdateCalendarEventRequest,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { type EventTextField } from 'src/app/shared/api/settings/event-text-fields-service';
import {
  applyCalendarEventDetailsRules,
  applyCalendarEventDefaultStart,
  createCalendarEventDetailsModel,
  eventTextFieldsToModel,
  formatScheduledStartUtcIso,
  patchCalendarEventDetailsModel,
  sameUpdateCalendarEventRequest,
  sameCalendarEventStartModel,
  scheduledStartUtcPreview,
  toCreateCalendarEventRequest,
  toUpdateCalendarEventRequest,
} from './calendar-event-details.form';

export class CalendarEventDraftState {
  private readonly _canUpdate = signal(true);
  private readonly savedRequest = signal<UpdateCalendarEventRequest | null>(null);
  private readonly loadedScheduledStartUtc = signal<string | null>(null);
  private readonly initialCreateStart;

  readonly model = signal(createCalendarEventDetailsModel());
  readonly canUpdate = this._canUpdate.asReadonly();
  readonly form = form(this.model, (path) =>
    applyCalendarEventDetailsRules(
      path,
      () => this.isEditMode,
      () => this._canUpdate(),
    ),
  );
  readonly hasPendingChanges = computed(() => {
    const saved = this.savedRequest();
    return (
      this.isEditMode &&
      this._canUpdate() &&
      saved !== null &&
      !sameUpdateCalendarEventRequest(this.updateRequest(), saved)
    );
  });
  readonly scheduledStartUtcDisplay = computed(() => {
    if (!this.isEditMode || this._canUpdate()) {
      const start = this.model().start;
      return scheduledStartUtcPreview(start.date, start.time, start.timeZoneId);
    }

    const loaded = this.loadedScheduledStartUtc();
    return loaded === null ? '' : formatScheduledStartUtcIso(loaded);
  });
  readonly startFutureError = computed(() => {
    const start = this.form.start();
    return start.touched() && start.errors().some((error) => error.kind === 'startInPast');
  });

  constructor(readonly isEditMode: boolean) {
    this._canUpdate.set(!isEditMode);
    this.initialCreateStart = { ...this.model().start };
  }

  applyCurrentFields(fields: readonly EventTextField[]): void {
    this.model.update((model) => ({
      ...model,
      start: { ...model.start },
      texts: eventTextFieldsToModel(fields),
    }));
  }

  applyDefaultStart(defaultStart: CalendarEventDefaultStart): void {
    if (
      this.isEditMode ||
      !sameCalendarEventStartModel(this.model().start, this.initialCreateStart)
    ) {
      return;
    }

    this.model.update((model) => ({
      ...model,
      start: applyCalendarEventDefaultStart(model.start, defaultStart),
    }));
  }

  applyEventDetails(event: CalendarEventDetailsResponse): void {
    patchCalendarEventDetailsModel(this.model, event);
    this.savedRequest.set(this.updateRequest());
    this.loadedScheduledStartUtc.set(event.scheduledStartUtc);
    this._canUpdate.set(event.canUpdate);
  }

  resetAfterLoadFailure(): void {
    this._canUpdate.set(false);
    this.savedRequest.set(null);
    this.loadedScheduledStartUtc.set(null);
  }

  validate(): boolean {
    if (this.form().valid()) {
      return true;
    }

    this.form().markAsTouched();
    return false;
  }

  createRequest(): CreateCalendarEventRequest {
    return toCreateCalendarEventRequest(this.model());
  }

  updateRequest(): UpdateCalendarEventRequest {
    return toUpdateCalendarEventRequest(this.model());
  }

  markSaved(request: UpdateCalendarEventRequest): void {
    this.savedRequest.set(request);
  }
}
