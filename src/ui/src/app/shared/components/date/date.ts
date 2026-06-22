import { Component, computed, effect, input, viewChild } from '@angular/core';
import { type Field } from '@angular/forms/signals';
import { ErrorStateMatcher } from '@angular/material/core';
import {
  MatDatepickerInputEvent,
  MatDatepickerModule,
} from '@angular/material/datepicker';
import { MatInput, MatInputModule } from '@angular/material/input';
import { DateTime } from 'luxon';

// Value contract: an ISO `YYYY-MM-DD` date string bound as a Signal Forms
// `Field<string>`. The Material datepicker uses Luxon `DateTime` internally,
// and this wrapper converts string<->DateTime at the Material boundary so pages
// and request mapping code never see adapter-specific date objects.
//
// Default (CheckAlways) change detection: the bound field exposes value,
// touched, and errors as signals, so the selected picker value and error
// message update reactively. See repo memory: "Frontend form-control wrappers".
@Component({
  selector: 'app-date',
  imports: [MatDatepickerModule, MatInputModule],
  templateUrl: './date.html',
  styleUrl: './date.scss'
})
export class DateField {
  /** Signal Forms field to bind. */
  readonly field = input.required<Field<string>>();
  readonly label = input('');

  private readonly input = viewChild(MatInput);

  protected readonly selectedDate = computed(() =>
    parseDateFieldValue(this.field()().value()),
  );

  protected readonly errorStateMatcher: ErrorStateMatcher = {
    isErrorState: () => this.errorMessage() !== null,
  };

  private readonly syncErrorState = effect(() => {
    this.errorMessage();
    this.input()?.updateErrorState();
  });

  /** First error message for the bound field, shown once the field is touched. */
  protected readonly errorMessage = computed(() => {
    const state = this.field()();
    if (!state.touched()) {
      return null;
    }

    return state.errors()[0]?.message ?? null;
  });

  protected onDateInput(event: MatDatepickerInputEvent<DateTime>): void {
    this.field()().value.set(formatDateFieldValue(event.value));
  }

  protected markAsTouched(): void {
    this.field()().markAsTouched();
  }
}

function parseDateFieldValue(value: string): DateTime | null {
  if (!value) {
    return null;
  }

  const date = DateTime.fromFormat(value, 'yyyy-MM-dd');
  return date.isValid ? date : null;
}

function formatDateFieldValue(value: DateTime | null): string {
  return value?.isValid ? value.toFormat('yyyy-MM-dd') : '';
}
