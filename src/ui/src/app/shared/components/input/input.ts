import {
  booleanAttribute,
  Component,
  computed,
  input,
} from '@angular/core';
import { FormField, type Field } from '@angular/forms/signals';
import { MatInputModule } from '@angular/material/input';

type InputType = 'text' | 'date' | 'time';

// Default (CheckAlways) change detection. The bound Signal Forms field exposes
// its value/touched/errors as signals, so the error message and character
// counter update reactively as the user types and when the page marks the form
// touched on submit. See repo memory:
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

  /**
   * Show the live "used / max" character counter below the field. Hidden by
   * default. The maximum comes from the bound field's `maxLength` rule in the
   * form schema, so there is nothing to show when the field has no length
   * limit.
   */
  readonly showCharacterCount = input(false, { transform: booleanAttribute });

  /**
   * The bound field's maximum string length from the form schema, or null when
   * the field has no length limit. This is the same limit Signal Forms applies
   * as the native character cap on the control.
   */
  protected readonly maxLength = computed(
    () => this.field()().maxLength?.() ?? null,
  );

  /** Live character count of the bound field value, recalculated as it changes. */
  protected readonly characterCount = computed(
    () => this.field()().value().length,
  );

  /** First error message for the bound field, shown once the field is touched. */
  protected readonly errorMessage = computed(() => {
    const state = this.field()();
    if (!state.touched()) {
      return null;
    }

    return state.errors()[0]?.message ?? null;
  });
}
