import { Component, computed, input, output, signal } from '@angular/core';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface ChipListItem {
  value: string;
  label: string;
  secondaryText?: string;
}

@Component({
  selector: 'app-chip-list',
  imports: [MatAutocompleteModule, MatChipsModule, MatFormFieldModule, MatInputModule],
  templateUrl: './chip-list.html',
  styleUrl: './chip-list.scss',
})
export class ChipList {
  readonly items = input<readonly ChipListItem[]>([]);
  readonly options = input<readonly ChipListItem[]>([]);
  readonly label = input('');
  readonly placeholder = input('');
  readonly disabled = input(false);

  readonly queryChange = output<string>();
  readonly itemSelected = output<ChipListItem>();
  readonly itemRemoved = output<string>();

  protected readonly query = signal('');
  protected readonly availableOptions = computed(() => {
    const selectedValues = new Set(this.items().map((item) => item.value));
    return this.options().filter((option) => !selectedValues.has(option.value));
  });

  onQueryInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.query.set(value);
    this.queryChange.emit(value.trim());
  }

  select(item: ChipListItem): void {
    if (this.disabled()) {
      return;
    }

    this.itemSelected.emit(item);
    this.query.set('');
  }

  onOptionSelected(event: MatAutocompleteSelectedEvent): void {
    this.select(event.option.value as ChipListItem);
  }

  remove(value: string): void {
    if (!this.disabled()) {
      this.itemRemoved.emit(value);
    }
  }

  protected itemAccessibleName(item: ChipListItem): string {
    return item.secondaryText === undefined
      ? item.label
      : `${item.label}, ${item.secondaryText}`;
  }
}
