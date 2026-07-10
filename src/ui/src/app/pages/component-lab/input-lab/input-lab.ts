import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { form, maxLength, required } from '@angular/forms/signals';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { Input } from 'src/app/shared/components/input/input';

interface InputLabModel {
  text: string;
  withPlaceholder: string;
  requiredText: string;
  limited: string;
  counted: string;
  multiline: string;
  multilineCounted: string;
  hoursBeforeEventStart: string;
}

@Component({
  selector: 'app-input-lab',
  imports: [Input, LabExample, LabPage],
  templateUrl: './input-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InputLab {
  protected readonly model = signal<InputLabModel>({
    text: 'Weekly stream check-in',
    withPlaceholder: '',
    requiredText: '',
    limited: 'Capped at forty characters',
    counted: 'Weekly stream check-in',
    multiline: 'First line\nSecond line',
    multilineCounted: 'Welcome to the weekly stream.',
    hoursBeforeEventStart: '24',
  });

  // A `maxLength` rule both caps the input and feeds the counter's denominator.
  // The counter itself is shown per field with `showCharacterCount`.
  protected readonly form = form(this.model, (path) => {
    required(path.requiredText, { message: 'Stream title is required' });
    maxLength(path.limited, 40, { message: 'Use 40 characters or fewer' });
    maxLength(path.counted, 40, { message: 'Use 40 characters or fewer' });
    maxLength(path.multilineCounted, 200, {
      message: 'Use 200 characters or fewer',
    });
  });

  constructor() {
    // Touch the required field so the example renders its error state on load.
    this.form.requiredText().markAsTouched();
  }
}
