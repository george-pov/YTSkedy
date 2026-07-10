import { Component, computed, effect, input, viewChild } from '@angular/core';
import { type Field } from '@angular/forms/signals';
import { ErrorStateMatcher } from '@angular/material/core';
import { MatInput, MatInputModule } from '@angular/material/input';
import { MatTimepickerModule } from '@angular/material/timepicker';
import { DateTime } from 'luxon';

import { TIME_INPUT_FORMAT } from 'src/app/shared/date-time/date-time-format';

// Value contract: an `HH:mm` time-of-day string bound as a Signal Forms
// `Field<string>`. The Material timepicker uses Luxon `DateTime` internally,
// and this wrapper converts string<->DateTime at the Material boundary so pages
// and request mapping code never see adapter-specific date objects.
//
// Default (CheckAlways) change detection: the bound field exposes value,
// touched, and errors as signals, so the selected picker value and error
// message update reactively. See repo memory: "Frontend form-control wrappers".
@Component({
  selector: 'app-time',
  imports: [MatInputModule, MatTimepickerModule],
  templateUrl: './time.html',
  styleUrl: './time.scss'
})
export class TimeField {
  /** Signal Forms field to bind. */
  readonly field = input.required<Field<string>>();
  readonly label = input('');

  private readonly input = viewChild(MatInput);

  protected readonly selectedTime = computed(() =>
    parseTimeFieldValue(this.field()().value()),
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

  protected onTimeSelected(value: DateTime): void {
    this.field()().value.set(formatTimeFieldValue(value));
  }

  protected onTimeValueChange(value: DateTime | null, inputValue: string): void {
    if (value === null && inputValue.length === 0) {
      this.field()().value.set('');
    }
  }

  protected onTimeInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const inputValue = sanitizeTimeInput(input.value);

    if (input.value !== inputValue) {
      input.value = inputValue;
    }

    if (inputValue.length === 0) {
      this.field()().value.set('');
      return;
    }

    const time = parseTimeFieldValue(inputValue);
    if (time !== null && formatTimeFieldValue(time) === inputValue) {
      this.field()().value.set(inputValue);
    }
  }

  protected onBlur(event: FocusEvent): void {
    (event.target as HTMLInputElement).value = this.field()().value();
    this.field()().markAsTouched();
  }
}

function parseTimeFieldValue(value: string): DateTime | null {
  if (!value) {
    return null;
  }

  const time = DateTime.fromFormat(value, TIME_INPUT_FORMAT);
  return time.isValid ? time : null;
}

function formatTimeFieldValue(value: DateTime | null): string {
  return value?.isValid ? value.toFormat(TIME_INPUT_FORMAT) : '';
}

function sanitizeTimeInput(value: string): string {
  return value.replace(/[^0-9:]/g, '').slice(0, 5);
}
