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
  type CalendarEventDetailsModel,
  createCalendarEventDetailsModel,
  eventTextFieldsToModel,
  formatScheduledStartUtcIso,
  patchCalendarEventDetailsModel,
  sameCreateCalendarEventRequest,
  sameUpdateCalendarEventRequest,
  sameCalendarEventStartModel,
  scheduledStartUtcPreview,
  toCreateCalendarEventRequest,
  toUpdateCalendarEventRequest,
} from './calendar-event-details.form';

export interface CalendarEventUpdateSubmission {
  readonly request: UpdateCalendarEventRequest;
  readonly submittedModel: CalendarEventDetailsModel;
}

export class CalendarEventDraftState {
  private readonly _canUpdate = signal(true);
  private readonly baselineModel = signal<CalendarEventDetailsModel | null>(null);
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
    const baseline = this.baselineModel();
    if (baseline === null) {
      return false;
    }

    return this.isEditMode
      ? this._canUpdate() &&
          !sameUpdateCalendarEventRequest(
            this.updateRequest(),
            toUpdateCalendarEventRequest(baseline),
          )
      : !sameCreateCalendarEventRequest(
          this.createRequest(),
          toCreateCalendarEventRequest(baseline),
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
    if (!isEditMode) {
      this.baselineModel.set(cloneCalendarEventDetailsModel(this.model()));
    }
  }

  applyCurrentFields(fields: readonly EventTextField[]): void {
    this.model.update((model) => ({
      ...model,
      texts: eventTextFieldsToModel(fields),
    }));
    this.baselineModel.update((baseline) =>
      baseline === null
        ? null
        : {
            ...baseline,
            texts: eventTextFieldsToModel(fields),
          },
    );
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
    this.baselineModel.update((baseline) =>
      baseline === null
        ? null
        : {
            ...baseline,
            start: applyCalendarEventDefaultStart(baseline.start, defaultStart),
          },
    );
  }

  applyEventDetails(event: CalendarEventDetailsResponse): void {
    patchCalendarEventDetailsModel(this.model, event);
    this.baselineModel.set(cloneCalendarEventDetailsModel(this.model()));
    this.loadedScheduledStartUtc.set(event.scheduledStartUtc);
    this._canUpdate.set(event.canUpdate);
  }

  resetAfterLoadFailure(): void {
    this._canUpdate.set(false);
    this.baselineModel.set(null);
    this.loadedScheduledStartUtc.set(null);
  }

  resetToBaseline(): void {
    const baseline = this.baselineModel();
    if (baseline === null) {
      return;
    }

    this.form().reset(cloneCalendarEventDetailsModel(baseline));
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

  captureUpdateSubmission(): CalendarEventUpdateSubmission {
    const submittedModel = cloneCalendarEventDetailsModel(this.model());
    return {
      request: toUpdateCalendarEventRequest(submittedModel),
      submittedModel,
    };
  }

  commitUpdateSubmission(submission: CalendarEventUpdateSubmission): void {
    this.baselineModel.set(cloneCalendarEventDetailsModel(submission.submittedModel));
  }

  clearPendingChangesForNavigation(): void {
    this.baselineModel.set(cloneCalendarEventDetailsModel(this.model()));
  }
}

function cloneCalendarEventDetailsModel(
  model: CalendarEventDetailsModel,
): CalendarEventDetailsModel {
  return {
    start: { ...model.start },
    texts: model.texts.map((text) => ({
      fieldKey: text.fieldKey,
      label: text.label,
      type: text.type,
      maxLength: text.maxLength,
      value: text.value,
    })),
  };
}
