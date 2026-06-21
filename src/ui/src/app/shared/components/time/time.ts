import { Component, computed, input } from '@angular/core';
import { FormField, type Field } from '@angular/forms/signals';
import { MatInputModule } from '@angular/material/input';

// Value contract: an `HH:mm` time-of-day string bound as a Signal Forms
// `Field<string>`. A future swap of the internals to an Angular Material
// timepicker (value type `Date`, requiring a DateAdapter and a date-library or
// native date adapter) converts string<->Date inside this wrapper at the
// Material boundary, keeping the outward `Field<string>` contract stable. The
// page and the request-mapping code never see a `Date`.
//
// Default (CheckAlways) change detection: the bound field exposes touched/errors
// as signals, so the error message updates reactively when the page marks the
// form touched on submit. See repo memory: "Frontend form-control wrappers".
@Component({
  selector: 'app-time',
  imports: [MatInputModule, FormField],
  templateUrl: './time.html',
  styleUrl: './time.scss'
})
export class TimeField {
  /** Signal Forms field to bind. */
  readonly field = input.required<Field<string>>();
  readonly label = input('');

  /** First error message for the bound field, shown once the field is touched. */
  protected readonly errorMessage = computed(() => {
    const state = this.field()();
    if (!state.touched()) {
      return null;
    }

    return state.errors()[0]?.message ?? null;
  });
}
