import { ChangeDetectionStrategy, Component } from '@angular/core';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { Toolbar } from 'src/app/shared/components/toolbar/toolbar';

@Component({
  selector: 'app-toolbar-lab',
  imports: [LabExample, LabPage, Toolbar],
  templateUrl: './toolbar-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToolbarLab {}
