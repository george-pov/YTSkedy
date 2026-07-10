import { Component, computed, effect, input, viewChild } from '@angular/core';
import { type Field } from '@angular/forms/signals';
import { ErrorStateMatcher } from '@angular/material/core';
import {
  MatDatepickerInputEvent,
  MatDatepickerModule,
} from '@angular/material/datepicker';
import { MatInput, MatInputModule } from '@angular/material/input';
import { DateTime } from 'luxon';

import {
  DATE_INPUT_DISPLAY_FORMAT,
  DATE_INPUT_FORMAT,
} from 'src/app/shared/date-time/date-time-format';

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
  protected readonly disabled = computed(() => this.field()().disabled());

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

  protected onDatePickerInput(event: MatDatepickerInputEvent<DateTime>): void {
    const date = event.value;
    const inputValue = (event.targetElement as HTMLInputElement).value;

    // Material formats picker selections before emitting `dateInput`. Typed
    // text remains in its raw form and is handled by `onDateTextInput` instead.
    if (date?.isValid && inputValue === formatDateDisplayValue(date)) {
      this.field()().value.set(formatDateFieldValue(date));
    }
  }

  protected onDateTextInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const inputValue = sanitizeDateInput(input.value);

    if (input.value !== inputValue) {
      input.value = inputValue;
    }

    if (inputValue.length === 0) {
      this.field()().value.set('');
      return;
    }

    const date = parseDateFieldValue(inputValue);
    if (date !== null && formatDateFieldValue(date) === inputValue) {
      this.field()().value.set(inputValue);
    }
  }

  protected prepareDateForEditing(event: KeyboardEvent): void {
    if (event.ctrlKey || event.metaKey || event.altKey) {
      return;
    }

    if (event.key.length === 1 && !/[0-9-]/.test(event.key)) {
      event.preventDefault();
      return;
    }

    if (
      !/[0-9-]/.test(event.key) &&
      event.key !== 'Backspace' &&
      event.key !== 'Delete'
    ) {
      return;
    }

    const input = event.target as HTMLInputElement;
    const date = parseDateFieldValue(this.field()().value());
    if (date === null || input.value !== formatDateDisplayValue(date)) {
      return;
    }

    const inputValue = formatDateFieldValue(date);
    const selectionStart = Math.min(
      input.selectionStart ?? inputValue.length,
      inputValue.length,
    );
    const selectionEnd = Math.min(
      input.selectionEnd ?? inputValue.length,
      inputValue.length,
    );
    input.value = inputValue;
    input.setSelectionRange(selectionStart, selectionEnd);
  }

  protected onBlur(event: FocusEvent): void {
    const input = event.target as HTMLInputElement;
    input.value = formatDateDisplayValue(
      parseDateFieldValue(this.field()().value()),
    );
    this.field()().markAsTouched();
  }
}

function parseDateFieldValue(value: string): DateTime | null {
  if (!value) {
    return null;
  }

  const date = DateTime.fromFormat(value, DATE_INPUT_FORMAT);
  return date.isValid ? date : null;
}

function formatDateFieldValue(value: DateTime | null): string {
  return value?.isValid ? value.toFormat(DATE_INPUT_FORMAT) : '';
}

function formatDateDisplayValue(value: DateTime | null): string {
  return value?.isValid ? value.toFormat(DATE_INPUT_DISPLAY_FORMAT) : '';
}

function sanitizeDateInput(value: string): string {
  return value.replace(/[^0-9-]/g, '').slice(0, 10);
}
