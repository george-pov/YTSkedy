import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { disabled, form } from '@angular/forms/signals';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { Checkbox } from 'src/app/shared/components/checkbox/checkbox';

interface CheckboxLabModel {
  unchecked: boolean;
  checked: boolean;
  disabled: boolean;
}

@Component({
  selector: 'app-checkbox-lab',
  imports: [Checkbox, LabExample, LabPage],
  templateUrl: './checkbox-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CheckboxLab {
  protected readonly model = signal<CheckboxLabModel>({
    unchecked: false,
    checked: true,
    disabled: false,
  });

  protected readonly form = form(this.model, (path) => {
    disabled(path.disabled, { when: () => true });
  });
}
