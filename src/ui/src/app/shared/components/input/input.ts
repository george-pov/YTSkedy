import { booleanAttribute, Component, computed, input } from '@angular/core';
import { FormField, type Field } from '@angular/forms/signals';
import { MatInputModule } from '@angular/material/input';

type InputType = 'text' | 'date' | 'time';

// Default (CheckAlways) change detection. The bound Signal Forms field exposes
// its touched/errors as signals, so the error message updates reactively when
// the page marks the form touched on submit. See repo memory:
// "Frontend form-control wrappers (leaf controls)".
@Component({
  selector: 'app-input',
  imports: [MatInputModule, FormField],
  templateUrl: './input.html',
  styleUrl: './input.scss'
})
export class Input {
  /** Signal Forms field to bind. */
  readonly field = input.required<Field<string>>();
  readonly label = input('');
  readonly type = input<InputType>('text');
  readonly placeholder = input('');

  /** Render a multi-line textarea instead of a single-line input. */
  readonly multiline = input(false, { transform: booleanAttribute });

  /** Visible rows for the multi-line textarea. */
  readonly rows = input(4);

  /** First error message for the bound field, shown once the field is touched. */
  protected readonly errorMessage = computed(() => {
    const state = this.field()();
    if (!state.touched()) {
      return null;
    }

    return state.errors()[0]?.message ?? null;
  });
}
