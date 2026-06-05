import { ChangeDetectionStrategy, Component } from '@angular/core';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { Button } from 'src/app/shared/components/button/button';

@Component({
  selector: 'app-button-lab',
  imports: [Button, LabExample, LabPage],
  templateUrl: './button-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ButtonLab {}
