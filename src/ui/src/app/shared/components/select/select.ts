import { booleanAttribute, Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';

export interface SelectOption {
  value: string;
  label: string;
}

// Follows the app-input leaf-wrapper pattern: takes a passed-in
// FormControl<string> bound with [formControl], exposes an errorMessages map,
// and relies on the page calling form.markAllAsTouched() to reveal errors on
// submit. Material directive and class names stay internal.
//
// Intentionally uses default (CheckAlways) change detection, not OnPush, so the
// view re-checks when the parent form is marked touched on submit. See repo
// memory: "Frontend form-control wrappers".
@Component({
  selector: 'app-select',
  imports: [MatFormFieldModule, MatSelectModule, ReactiveFormsModule],
  templateUrl: './select.html',
  styleUrl: './select.scss'
})
export class Select {
  readonly control = input.required<FormControl<string>>();
  readonly label = input('');
  readonly options = input<readonly SelectOption[]>([]);
  readonly required = input(false, { transform: booleanAttribute });

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
