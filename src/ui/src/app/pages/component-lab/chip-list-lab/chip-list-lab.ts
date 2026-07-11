import { ChangeDetectionStrategy, Component, signal } from '@angular/core';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { ChipList, ChipListItem } from 'src/app/shared/components/chip-list/chip-list';

@Component({
  selector: 'app-chip-list-lab',
  imports: [ChipList, LabExample, LabPage],
  templateUrl: './chip-list-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChipListLab {
  protected readonly selectedItems = signal<readonly ChipListItem[]>([
    { value: '12', label: 'Events', secondaryText: '#12' },
    { value: '34', label: 'News', secondaryText: '#34' },
  ]);
  protected readonly availableItems = signal<readonly ChipListItem[]>([
    { value: '12', label: 'Events', secondaryText: '#12' },
    { value: '34', label: 'News', secondaryText: '#34' },
    { value: '56', label: 'Livestreams', secondaryText: '#56' },
    { value: '78', label: 'Announcements', secondaryText: '#78' },
  ]);
  protected readonly searchText = signal('');

  protected onQueryChange(search: string): void {
    this.searchText.set(search);
  }

  protected selectItem(item: ChipListItem): void {
    if (!this.selectedItems().some((selected) => selected.value === item.value)) {
      this.selectedItems.update((items) => [...items, item]);
    }
  }

  protected removeItem(value: string): void {
    this.selectedItems.update((items) => items.filter((item) => item.value !== value));
  }
}
