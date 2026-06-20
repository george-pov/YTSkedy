import { Component, computed, input } from '@angular/core';
import { FormField, type Field } from '@angular/forms/signals';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';

export interface SelectOption {
  value: string;
  label: string;
}

// Follows the app-input leaf-wrapper pattern: takes a Signal Forms
// `Field<string>` bound with [formField] and renders the first error message
// once the field is touched. Material directive and class names stay internal.
//
// Default (CheckAlways) change detection: the bound field exposes touched/errors
// as signals, so the error message updates reactively when the page marks the
// form touched on submit. See repo memory: "Frontend form-control wrappers".
@Component({
  selector: 'app-select',
  imports: [MatFormFieldModule, MatSelectModule, FormField],
  templateUrl: './select.html',
  styleUrl: './select.scss'
})
export class Select {
  /** Signal Forms field to bind. */
  readonly field = input.required<Field<string>>();
  readonly label = input('');
  readonly options = input<readonly SelectOption[]>([]);

  /** First error message for the bound field, shown once the field is touched. */
  protected readonly errorMessage = computed(() => {
    const state = this.field()();
    if (!state.touched()) {
      return null;
    }

    return state.errors()[0]?.message ?? null;
  });
}
