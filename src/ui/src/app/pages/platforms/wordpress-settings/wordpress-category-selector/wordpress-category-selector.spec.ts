import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { By } from '@angular/platform-browser';
import { finalize, Observable, of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import {
  PlatformsService,
  WordPressCategoryListResponse,
  WordPressCategoryQuery,
} from 'src/app/shared/api/platforms/platforms-service';
import { ChipListItem } from 'src/app/shared/components/chip-list/chip-list';
import { WordPressCategorySelector } from './wordpress-category-selector';

@Component({
  selector: 'app-wordpress-category-selector-host',
  imports: [WordPressCategorySelector],
  template: `<app-wordpress-category-selector
    [platformId]="platformId()"
    [categoryIds]="form.categoryIds"
  />`,
})
class WordPressCategorySelectorHost {
  readonly platformId = signal<string | null>(null);
  readonly model = signal({ categoryIds: [] as number[] });
  readonly form = form(this.model, () => {});
}

describe('WordPressCategorySelector', () => {
  let fixture: ComponentFixture<WordPressCategorySelectorHost>;
  let host: WordPressCategorySelectorHost;
  let listCategories: Mock<
    (platformId: string, query: WordPressCategoryQuery) => Observable<WordPressCategoryListResponse>
  >;

  beforeEach(() => {
    listCategories = vi.fn();
    listCategories.mockReturnValue(of(page([])));
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: PlatformsService,
          useValue: { listWordPressCategories: listCategories },
        },
      ],
    });
    fixture = TestBed.createComponent(WordPressCategorySelectorHost);
    host = fixture.componentInstance;
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('shows save-first guidance in create mode without issuing requests', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'Save the WordPress platform before choosing categories.',
    );
    expect(listCategories).not.toHaveBeenCalled();
  });

  it('loads selected category labels and sends no credential fields', async () => {
    host.platformId.set('wp-1');
    host.model.set({ categoryIds: [12] });
    listCategories.mockReturnValue(of(page([{ id: 12, name: 'Events', slug: 'events' }], 1, 1)));

    await render();

    expect(fixture.nativeElement.textContent).toContain('Events');
    expect(fixture.nativeElement.textContent).toContain('#12');
    expect(listCategories).toHaveBeenCalledWith('wp-1', {
      includeIds: [12],
      page: 1,
      pageSize: 100,
    });
    expect(JSON.stringify(listCategories.mock.calls)).not.toContain('applicationPassword');
    expect(JSON.stringify(listCategories.mock.calls)).not.toContain('username');
  });

  it('unsubscribes from a pending selected-category load when destroyed', async () => {
    const response = new Subject<WordPressCategoryListResponse>();
    const teardown = vi.fn();
    listCategories.mockReturnValue(response.pipe(finalize(teardown)));
    host.platformId.set('wp-1');
    host.model.set({ categoryIds: [12] });

    await render();

    expect(teardown).not.toHaveBeenCalled();
    fixture.destroy();
    expect(teardown).toHaveBeenCalledTimes(1);

    response.next(page([{ id: 12, name: 'Late', slug: 'late' }], 1, 1));
    response.error(new Error('late failure'));
    expect(listCategories).toHaveBeenCalledTimes(1);
  });

  it('loads every selected-category page', async () => {
    host.platformId.set('wp-1');
    host.model.set({ categoryIds: [12, 34] });
    listCategories.mockImplementation((_platformId, query) =>
      query.page === 1
        ? of(page([{ id: 12, name: 'Events', slug: 'events' }], 1, 2))
        : of(page([{ id: 34, name: 'News', slug: 'news' }], 2, 2)),
    );

    await render();

    expect(fixture.nativeElement.textContent).toContain('Events');
    expect(fixture.nativeElement.textContent).toContain('News');
    expect(listCategories).toHaveBeenNthCalledWith(2, 'wp-1', {
      includeIds: [12, 34],
      page: 2,
      pageSize: 100,
    });
  });

  it('keeps fallback labels and removal available after selected lookup failure', async () => {
    host.platformId.set('wp-1');
    host.model.set({ categoryIds: [12] });
    listCategories.mockReturnValue(throwError(() => new Error('provider failed')));

    await render();

    expect(fixture.nativeElement.textContent).toContain('Category #12');
    expect(fixture.nativeElement.textContent).toContain(
      'Categories could not be loaded. Try again.',
    );
    const remove = fixture.nativeElement.querySelector(
      'button[aria-label="Remove Category #12, #12"]',
    ) as HTMLButtonElement;
    expect(remove.disabled).toBe(false);
  });

  it('clears a selected lookup error after the last category is removed', async () => {
    host.platformId.set('wp-1');
    host.model.set({ categoryIds: [12] });
    listCategories.mockReturnValue(throwError(() => new Error('provider failed')));

    await render();
    expect(fixture.nativeElement.textContent).toContain(
      'Categories could not be loaded. Try again.',
    );

    selector().removeItem('12');
    await render();

    expect(fixture.nativeElement.textContent).not.toContain(
      'Categories could not be loaded. Try again.',
    );
  });

  it('debounces non-empty search and exposes returned options', async () => {
    vi.useFakeTimers();
    host.platformId.set('wp-1');
    await render();
    listCategories.mockClear();
    listCategories.mockReturnValue(of(page([{ id: 34, name: 'News', slug: 'news' }], 1, 1)));

    selector().onQueryChange('  news  ');
    await vi.advanceTimersByTimeAsync(249);
    expect(listCategories).not.toHaveBeenCalled();
    await vi.advanceTimersByTimeAsync(1);
    fixture.detectChanges();

    expect(listCategories).toHaveBeenCalledWith('wp-1', {
      search: 'news',
      page: 1,
      pageSize: 25,
    });
    expect(optionItems()).toEqual([{ value: '34', label: 'News', secondaryText: '#34' }]);

    listCategories.mockClear();
    selector().onQueryChange('   ');
    await vi.advanceTimersByTimeAsync(250);
    expect(listCategories).not.toHaveBeenCalled();
  });

  it('selects once and removes only the requested ID while preserving order', async () => {
    host.platformId.set('wp-1');
    host.model.set({ categoryIds: [12, 56] });
    await render();

    const item: ChipListItem = { value: '34', label: 'News', secondaryText: '#34' };
    selector().selectItem(item);
    selector().selectItem(item);
    selector().selectItem({ value: 'invalid', label: 'Invalid' });
    expect(host.model().categoryIds).toEqual([12, 56, 34]);

    selector().removeItem('56');
    expect(host.model().categoryIds).toEqual([12, 34]);
  });

  it('ignores a stale selected-label response after IDs change', async () => {
    const first = new Subject<WordPressCategoryListResponse>();
    const second = new Subject<WordPressCategoryListResponse>();
    listCategories.mockReturnValueOnce(first).mockReturnValueOnce(second);
    host.platformId.set('wp-1');
    host.model.set({ categoryIds: [12] });
    await render();

    host.model.set({ categoryIds: [34] });
    fixture.detectChanges();
    await fixture.whenStable();
    first.next(page([{ id: 12, name: 'Old', slug: 'old' }], 1, 1));
    second.next(page([{ id: 34, name: 'Current', slug: 'current' }], 1, 1));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Old');
    expect(fixture.nativeElement.textContent).toContain('Current');
  });

  it('ignores a stale search response after search text changes', async () => {
    vi.useFakeTimers();
    const first = new Subject<WordPressCategoryListResponse>();
    const second = new Subject<WordPressCategoryListResponse>();
    host.platformId.set('wp-1');
    await render();
    listCategories.mockReturnValueOnce(first).mockReturnValueOnce(second);

    selector().onQueryChange('old');
    await vi.advanceTimersByTimeAsync(250);
    selector().onQueryChange('current');
    await vi.advanceTimersByTimeAsync(250);
    first.next(page([{ id: 12, name: 'Old', slug: 'old' }], 1, 1));
    second.next(page([{ id: 34, name: 'Current', slug: 'current' }], 1, 1));

    expect(optionItems()).toEqual([{ value: '34', label: 'Current', secondaryText: '#34' }]);
  });

  it('ignores a selected-label response after switching platforms', async () => {
    const first = new Subject<WordPressCategoryListResponse>();
    const second = new Subject<WordPressCategoryListResponse>();
    listCategories.mockReturnValueOnce(first).mockReturnValueOnce(second);
    host.platformId.set('wp-1');
    host.model.set({ categoryIds: [12] });
    await render();

    host.platformId.set('wp-2');
    host.model.set({ categoryIds: [34] });
    fixture.detectChanges();
    await fixture.whenStable();
    first.next(page([{ id: 12, name: 'First site', slug: 'first' }], 1, 1));
    second.next(page([{ id: 34, name: 'Second site', slug: 'second' }], 1, 1));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('First site');
    expect(fixture.nativeElement.textContent).toContain('Second site');
  });

  function selector(): WordPressCategorySelector {
    return fixture.debugElement.query(By.directive(WordPressCategorySelector)).componentInstance;
  }

  function optionItems(): readonly ChipListItem[] {
    return (selector() as unknown as { optionItems: () => readonly ChipListItem[] }).optionItems();
  }

  async function render(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function page(
    items: WordPressCategoryListResponse['items'],
    currentPage = 1,
    totalPages = items.length === 0 ? 0 : 1,
  ): WordPressCategoryListResponse {
    return {
      items,
      page: currentPage,
      pageSize: 100,
      total: items.length,
      totalPages,
    };
  }
});
