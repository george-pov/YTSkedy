import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { form, maxLength, required } from '@angular/forms/signals';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { MaskedInput } from 'src/app/shared/components/masked-input/masked-input';

interface MaskedInputLabModel {
  secret: string;
  password: string;
  replacement: string;
}

@Component({
  selector: 'app-masked-input-lab',
  imports: [LabExample, LabPage, MaskedInput],
  templateUrl: './masked-input-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MaskedInputLab {
  protected readonly model = signal<MaskedInputLabModel>({
    secret: '',
    password: '',
    replacement: 'replacement-secret-N3W',
  });

  protected readonly form = form(this.model, (path) => {
    required(path.secret, { message: 'Client secret is required' });
    maxLength(path.secret, 256, { message: 'Use 256 characters or fewer' });
    maxLength(path.password, 512, { message: 'Use 512 characters or fewer' });
    maxLength(path.replacement, 256, {
      message: 'Use 256 characters or fewer',
    });
  });
}
