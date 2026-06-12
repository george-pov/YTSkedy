import { Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';

// Value contract: an `HH:mm` time-of-day string. A future swap of the
// internals to an Angular Material timepicker (value type `Date`, requiring a
// DateAdapter and a date-library or native date adapter) converts
// string<->Date inside this wrapper at the Material boundary, keeping the
// outward FormControl<string> contract stable. The page and the
// request-mapping code never see a `Date`.
//
// Intentionally uses default (CheckAlways) change detection, not OnPush, for
// the same reason as app-input: the page reveals validation errors on submit
// via form.markAllAsTouched(); under OnPush this view would not re-check on a
// parent-form submit. See repo memory: "Frontend form-control wrappers".
@Component({
  selector: 'app-time',
  imports: [MatInputModule, ReactiveFormsModule],
  templateUrl: './time.html',
  styleUrl: './time.scss'
})
export class TimeField {
  readonly control = input.required<FormControl<string>>();
  readonly label = input('');

  /** Maps validator error keys to user-facing messages, e.g. `{ required: '...' }`. */
  readonly errorMessages = input<Record<string, string>>({});

  protected currentErrorMessage(): string | null {
    const errors = this.control().errors;
    if (!errors) {
      return null;
    }

    const messages = this.errorMessages();
    const firstKey = Object.keys(errors)[0];
    return messages[firstKey] ?? null;
  }
}
