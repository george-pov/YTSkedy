import { ChangeDetectionStrategy, Component } from '@angular/core';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { StatusPill } from 'src/app/shared/components/status-pill/status-pill';

@Component({
  selector: 'app-status-pill-lab',
  imports: [LabExample, LabPage, StatusPill],
  templateUrl: './status-pill-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusPillLab {}
