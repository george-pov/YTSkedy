import { Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';

type InputType = 'text' | 'date' | 'time';

// Intentionally uses default (CheckAlways) change detection, not OnPush.
// The page reveals validation errors on submit via form.markAllAsTouched();
// under OnPush this view would not re-check on a parent-form submit, so the
// error would not appear without extra markForCheck wiring. Default CD keeps
// the passed-in FormControl's error state visible for free. See repo memory:
// "Frontend form-control wrappers (leaf controls)".
@Component({
  selector: 'app-input',
  imports: [MatInputModule, ReactiveFormsModule],
  templateUrl: './input.html',
  styleUrl: './input.scss'
})
export class Input {
  readonly control = input.required<FormControl<string>>();
  readonly label = input('');
  readonly type = input<InputType>('text');
  readonly placeholder = input('');

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
