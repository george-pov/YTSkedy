import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { DateField } from 'src/app/shared/components/date/date';
import { Select, SelectOption } from 'src/app/shared/components/select/select';
import { TimeField } from 'src/app/shared/components/time/time';

@Component({
  selector: 'app-form-controls-lab',
  imports: [DateField, TimeField, Select, LabExample, LabPage],
  templateUrl: './form-controls-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormControlsLab {
  protected readonly dateControl = new FormControl('2026-06-06', {
    nonNullable: true,
  });
  protected readonly timeControl = new FormControl('10:00', {
    nonNullable: true,
  });
  protected readonly timeZoneControl = new FormControl('UTC', {
    nonNullable: true,
  });

  protected readonly requiredDateControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required],
  });
  protected readonly requiredTimeControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required],
  });
  protected readonly requiredTimeZoneControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required],
  });

  protected readonly timeZoneOptions: SelectOption[] = [
    { value: 'America/Vancouver', label: 'Vancouver' },
    { value: 'Europe/London', label: 'London' },
    { value: 'Europe/Moscow', label: 'Moscow' },
    { value: 'UTC', label: 'UTC' },
  ];

  protected readonly dateErrors = { required: 'Date is required' };
  protected readonly timeErrors = { required: 'Time is required' };
  protected readonly timeZoneErrors = { required: 'Time zone is required' };

  constructor() {
    this.requiredDateControl.markAsTouched();
    this.requiredTimeControl.markAsTouched();
    this.requiredTimeZoneControl.markAsTouched();
  }
}
