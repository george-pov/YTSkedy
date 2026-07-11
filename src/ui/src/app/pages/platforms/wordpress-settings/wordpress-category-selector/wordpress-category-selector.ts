import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type Field } from '@angular/forms/signals';

import {
  PlatformsService,
  WordPressCategory,
} from 'src/app/shared/api/platforms/platforms-service';
import { ChipList, ChipListItem } from 'src/app/shared/components/chip-list/chip-list';

const SearchDebounceMs = 250;
const SearchPageSize = 25;
const SelectedPageSize = 100;

@Component({
  selector: 'app-wordpress-category-selector',
  imports: [ChipList],
  templateUrl: './wordpress-category-selector.html',
  styleUrl: './wordpress-category-selector.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordPressCategorySelector {
  readonly platformId = input<string | null>(null);
  readonly categoryIds = input.required<Field<number[]>>();

  private readonly platformsService = inject(PlatformsService);
  private readonly destroyRef = inject(DestroyRef);
  private selectedRequestId = 0;
  private searchRequestId = 0;
  private searchTimer: ReturnType<typeof setTimeout> | null = null;
  private activeRequestCount = 0;
  private selectionKey = '';

  protected readonly searchText = signal('');
  protected readonly selectedCategories = signal<readonly WordPressCategory[]>([]);
  protected readonly searchResults = signal<readonly WordPressCategory[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly selectedItems = computed<readonly ChipListItem[]>(() => {
    const categories = new Map(
      this.selectedCategories().map((category) => [category.id, category] as const),
    );

    return this.categoryIds()()
      .value()
      .map((id) => {
        const category = categories.get(id);
        return {
          value: id.toString(),
          label: category?.name ?? `Category #${id}`,
          secondaryText: `#${id}`,
        };
      });
  });

  protected readonly optionItems = computed<readonly ChipListItem[]>(() =>
    this.searchResults().map((category) => ({
      value: category.id.toString(),
      label: category.name,
      secondaryText: `#${category.id}`,
    })),
  );

  constructor() {
    effect(() => {
      const platformId = this.platformId();
      const ids = this.categoryIds()().value();
      const selectionKey = `${platformId ?? ''}:${ids.join(',')}`;
      if (selectionKey === this.selectionKey) {
        return;
      }

      this.selectionKey = selectionKey;
      this.cancelPendingSearch();
      this.searchText.set('');
      this.searchResults.set([]);
      this.loadSelectedCategories();
    });

    this.destroyRef.onDestroy(() => this.cancelPendingSearch());
  }

  onQueryChange(search: string): void {
    const trimmed = search.trim();
    this.cancelPendingSearch();
    this.searchText.set(trimmed);
    this.searchResults.set([]);

    if (trimmed.length === 0 || this.platformId() === null) {
      return;
    }

    this.searchTimer = setTimeout(() => {
      this.searchTimer = null;
      this.searchCategories(trimmed);
    }, SearchDebounceMs);
  }

  selectItem(item: ChipListItem): void {
    const categoryId = Number(item.value);
    const ids = this.categoryIds()().value();
    if (!Number.isSafeInteger(categoryId) || categoryId <= 0 || ids.includes(categoryId)) {
      return;
    }

    const category = this.searchResults().find((candidate) => candidate.id === categoryId);
    if (category !== undefined) {
      this.selectedCategories.update((categories) => [...categories, category]);
    }
    this.categoryIds()().value.set([...ids, categoryId]);
    this.cancelPendingSearch();
    this.searchText.set('');
    this.searchResults.set([]);
  }

  removeItem(value: string): void {
    const categoryId = Number(value);
    if (!Number.isSafeInteger(categoryId) || categoryId <= 0) {
      return;
    }

    const ids = this.categoryIds()().value();
    this.categoryIds()().value.set(ids.filter((id) => id !== categoryId));
    this.selectedCategories.update((categories) =>
      categories.filter((category) => category.id !== categoryId),
    );
  }

  loadSelectedCategories(): void {
    const platformId = this.platformId();
    const ids = [...this.categoryIds()().value()];
    const selectionKey = `${platformId ?? ''}:${ids.join(',')}`;
    const requestId = ++this.selectedRequestId;
    this.selectedCategories.set([]);

    if (platformId === null || ids.length === 0) {
      this.errorMessage.set(null);
      return;
    }

    this.beginRequest();
    const categories: WordPressCategory[] = [];
    const loadPage = (page: number): void => {
      this.platformsService
        .listWordPressCategories(platformId, {
          includeIds: ids,
          page,
          pageSize: SelectedPageSize,
        })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (response) => {
            if (!this.isSelectedRequestCurrent(requestId, platformId, selectionKey)) {
              this.endRequest();
              return;
            }

            categories.push(...response.items);
            if (page < response.totalPages) {
              loadPage(page + 1);
              return;
            }

            this.selectedCategories.set(uniqueCategories(categories));
            this.errorMessage.set(null);
            this.endRequest();
          },
          error: () => {
            if (this.isSelectedRequestCurrent(requestId, platformId, selectionKey)) {
              this.selectedCategories.set([]);
              this.errorMessage.set('Categories could not be loaded. Try again.');
            }
            this.endRequest();
          },
        });
    };

    loadPage(1);
  }

  searchCategories(search: string): void {
    const platformId = this.platformId();
    const requestId = ++this.searchRequestId;
    if (platformId === null || search.length === 0) {
      return;
    }

    this.beginRequest();
    this.platformsService
      .listWordPressCategories(platformId, {
        search,
        page: 1,
        pageSize: SearchPageSize,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          if (this.isSearchRequestCurrent(requestId, platformId, search)) {
            this.searchResults.set(response.items);
            this.errorMessage.set(null);
          }
          this.endRequest();
        },
        error: () => {
          if (this.isSearchRequestCurrent(requestId, platformId, search)) {
            this.searchResults.set([]);
            this.errorMessage.set('Categories could not be loaded. Try again.');
          }
          this.endRequest();
        },
      });
  }

  private isSelectedRequestCurrent(
    requestId: number,
    platformId: string,
    selectionKey: string,
  ): boolean {
    return (
      requestId === this.selectedRequestId &&
      platformId === this.platformId() &&
      selectionKey === `${this.platformId() ?? ''}:${this.categoryIds()().value().join(',')}`
    );
  }

  private isSearchRequestCurrent(
    requestId: number,
    platformId: string,
    search: string,
  ): boolean {
    return (
      requestId === this.searchRequestId &&
      platformId === this.platformId() &&
      search === this.searchText()
    );
  }

  private cancelPendingSearch(): void {
    this.searchRequestId++;
    if (this.searchTimer !== null) {
      clearTimeout(this.searchTimer);
      this.searchTimer = null;
    }
  }

  private beginRequest(): void {
    this.activeRequestCount++;
    this.isLoading.set(true);
  }

  private endRequest(): void {
    this.activeRequestCount = Math.max(0, this.activeRequestCount - 1);
    this.isLoading.set(this.activeRequestCount > 0);
  }
}

function uniqueCategories(categories: readonly WordPressCategory[]): WordPressCategory[] {
  const seen = new Set<number>();
  return categories.filter((category) => !seen.has(category.id) && seen.add(category.id));
}
