import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ChipList, ChipListItem } from './chip-list';

describe('ChipList', () => {
  let fixture: ComponentFixture<ChipList>;

  const selected: ChipListItem[] = [
    { value: '12', label: 'Events', secondaryText: '#12' },
  ];
  const options: ChipListItem[] = [
    { value: '12', label: 'Events', secondaryText: '#12' },
    { value: '34', label: 'News', secondaryText: '#34' },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    fixture = TestBed.createComponent(ChipList);
    fixture.componentRef.setInput('label', 'Categories');
    fixture.componentRef.setInput('placeholder', 'Search categories');
    fixture.componentRef.setInput('items', selected);
    fixture.componentRef.setInput('options', options);
    fixture.detectChanges();
  });

  it('renders the label, placeholder, selected item, and secondary text', () => {
    const element = fixture.nativeElement as HTMLElement;
    const input = element.querySelector('input') as HTMLInputElement;

    expect(element.textContent).toContain('Categories');
    expect(input.placeholder).toBe('Search categories');
    expect(element.textContent).toContain('Events');
    expect(element.textContent).toContain('#12');
  });

  it('emits trimmed query text while retaining the typed input value', () => {
    const emitted = vi.fn();
    fixture.componentInstance.queryChange.subscribe(emitted);
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;

    input.value = '  news  ';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(emitted).toHaveBeenCalledWith('news');
    expect(input.value).toBe('  news  ');
  });

  it('selects an item and clears the local query', () => {
    const emitted = vi.fn();
    fixture.componentInstance.itemSelected.subscribe(emitted);
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = 'news';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    fixture.componentInstance.select(options[1]);
    fixture.detectChanges();

    expect(emitted).toHaveBeenCalledWith(options[1]);
    expect(input.value).toBe('');
  });

  it('emits removal without mutating controlled items', () => {
    const emitted = vi.fn();
    fixture.componentInstance.itemRemoved.subscribe(emitted);

    fixture.componentInstance.remove('12');

    expect(emitted).toHaveBeenCalledWith('12');
    expect(fixture.componentInstance.items()).toEqual(selected);
  });

  it('filters already-selected values from autocomplete options', () => {
    const availableOptions = (
      fixture.componentInstance as unknown as { availableOptions: () => readonly ChipListItem[] }
    ).availableOptions();

    expect(availableOptions).toEqual([options[1]]);
  });

  it('disables input and ignores selection and removal while disabled', () => {
    const selectedEvent = vi.fn();
    const removedEvent = vi.fn();
    fixture.componentInstance.itemSelected.subscribe(selectedEvent);
    fixture.componentInstance.itemRemoved.subscribe(removedEvent);
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();

    fixture.componentInstance.select(options[1]);
    fixture.componentInstance.remove('12');

    expect((fixture.nativeElement.querySelector('input') as HTMLInputElement).disabled).toBe(true);
    expect(selectedEvent).not.toHaveBeenCalled();
    expect(removedEvent).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Events');
  });

  it('provides accessible input, chip, and remove-button names', () => {
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('input')?.getAttribute('aria-label')).toBe('Categories');
    expect(element.querySelector('[aria-label="Events, #12"]')).not.toBeNull();
    expect(element.querySelector('button[matChipRemove]')?.getAttribute('aria-label')).toBe(
      'Remove Events, #12',
    );
  });
});
