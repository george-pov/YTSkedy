import { ChangeDetectionStrategy, Component, signal } from '@angular/core';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { UserBadge } from 'src/app/shared/components/user-badge/user-badge';

@Component({
  selector: 'app-user-badge-lab',
  imports: [LabExample, LabPage, UserBadge],
  templateUrl: './user-badge-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserBadgeLab {
  protected readonly lastSignOut = signal<string | null>(null);
}
