import { ChangeDetectionStrategy, Component, signal } from '@angular/core';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import {
  ToolbarNav,
  ToolbarNavItem,
} from 'src/app/shared/components/toolbar-nav/toolbar-nav';

@Component({
  selector: 'app-toolbar-nav-lab',
  imports: [LabExample, LabPage, ToolbarNav],
  templateUrl: './toolbar-nav-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToolbarNavLab {
  protected readonly linkItems: readonly ToolbarNavItem[] = [
    { label: 'Calendar Events', link: '/calendar-events' },
    { label: 'Settings', link: '/settings' },
    { label: 'Templates', link: '/templates' },
  ];

  protected readonly lastAction = signal<string | null>(null);

  protected readonly actionItems: readonly ToolbarNavItem[] = [
    {
      label: 'Calendar Events',
      action: () => this.lastAction.set('Calendar Events'),
    },
    { label: 'Settings', action: () => this.lastAction.set('Settings') },
    { label: 'Templates', action: () => this.lastAction.set('Templates') },
  ];
}
