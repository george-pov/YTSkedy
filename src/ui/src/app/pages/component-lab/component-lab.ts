import { NgComponentOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';

import { ComponentLabItem, componentLabItems } from './component-lab.registry';

@Component({
  selector: 'app-component-lab',
  imports: [
    NgComponentOutlet,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatMenuModule,
  ],
  templateUrl: './component-lab.html',
  styleUrl: './component-lab.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComponentLab {
  protected readonly labItems = componentLabItems;

  // Which lab is shown on the right. Defaults to the first registered item so
  // the page is never empty on load.
  protected readonly selected = signal<ComponentLabItem>(componentLabItems[0]);

  protected select(item: ComponentLabItem): void {
    this.selected.set(item);
  }
}
