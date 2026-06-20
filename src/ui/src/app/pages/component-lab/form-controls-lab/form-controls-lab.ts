import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { form, required } from '@angular/forms/signals';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { DateField } from 'src/app/shared/components/date/date';
import { Select, SelectOption } from 'src/app/shared/components/select/select';
import { TimeField } from 'src/app/shared/components/time/time';

interface FormControlsLabModel {
  date: string;
  requiredDate: string;
  time: string;
  requiredTime: string;
  timeZone: string;
  requiredTimeZone: string;
}

@Component({
  selector: 'app-form-controls-lab',
  imports: [DateField, TimeField, Select, LabExample, LabPage],
  templateUrl: './form-controls-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormControlsLab {
  protected readonly model = signal<FormControlsLabModel>({
    date: '2026-06-06',
    requiredDate: '',
    time: '10:00',
    requiredTime: '',
    timeZone: 'UTC',
    requiredTimeZone: '',
  });

  protected readonly form = form(this.model, (path) => {
    required(path.requiredDate, { message: 'Date is required' });
    required(path.requiredTime, { message: 'Time is required' });
    required(path.requiredTimeZone, { message: 'Time zone is required' });
  });

  protected readonly timeZoneOptions: SelectOption[] = [
    { value: 'America/Vancouver', label: 'Vancouver' },
    { value: 'Europe/London', label: 'London' },
    { value: 'Europe/Moscow', label: 'Moscow' },
    { value: 'UTC', label: 'UTC' },
  ];

  constructor() {
    this.form.requiredDate().markAsTouched();
    this.form.requiredTime().markAsTouched();
    this.form.requiredTimeZone().markAsTouched();
  }
}
