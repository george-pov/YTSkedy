import { Component, input } from '@angular/core';
import { FormField, type Field } from '@angular/forms/signals';
import { MatCheckboxModule } from '@angular/material/checkbox';

@Component({
  selector: 'app-checkbox',
  imports: [FormField, MatCheckboxModule],
  templateUrl: './checkbox.html',
  styleUrl: './checkbox.scss',
})
export class Checkbox {
  readonly field = input.required<Field<boolean>>();
  readonly label = input('');
}
