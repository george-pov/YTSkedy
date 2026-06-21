import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { Templates } from './templates';
import { Template } from './templates.form';

describe('Templates', () => {
  let fixture: ComponentFixture<Templates>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(Templates);
    fixture.detectChanges();
  });

  function state(): { templates: () => Template[]; selectedId: () => number | null } {
    return fixture.componentInstance as unknown as {
      templates: () => Template[];
      selectedId: () => number | null;
    };
  }

  function rows(): HTMLElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('tr')).filter(
      (row) => (row as HTMLElement).querySelector('td') !== null,
    ) as HTMLElement[];
  }

  function editor(): HTMLElement | null {
    return fixture.nativeElement.querySelector('form.editor');
  }

  function buttonByText(text: string): HTMLButtonElement {
    return Array.from(
      fixture.nativeElement.querySelectorAll('app-button button'),
    ).find((button) =>
      ((button as HTMLElement).textContent ?? '').trim().includes(text),
    ) as HTMLButtonElement;
  }

  async function selectRow(index: number): Promise<void> {
    rows()[index].dispatchEvent(new Event('click'));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders a row per template with type and name columns', () => {
    const headers = Array.from(fixture.nativeElement.querySelectorAll('th')).map(
      (th) => (th as HTMLElement).textContent?.trim(),
    );
    expect(headers).toEqual(['Type', 'Name']);

    expect(rows()).toHaveLength(3);
    const firstCells = rows()[0].querySelectorAll('td');
    expect(firstCells[0].textContent?.trim()).toBe('YouTube');
    expect(firstCells[1].textContent?.trim()).toBe('Weekly live stream');
  });

  it('hides the editor until a template is selected', () => {
    expect(editor()).toBeNull();
  });

  it('shows the selected template values in the editor', async () => {
    await selectRow(0);

    expect(editor()).not.toBeNull();
    const inputs = fixture.nativeElement.querySelectorAll('app-input input');
    const textarea = fixture.nativeElement.querySelector('app-input textarea');
    expect((inputs[0] as HTMLInputElement).value).toBe('YouTube');
    expect((inputs[1] as HTMLInputElement).value).toBe('Weekly live stream');
    expect((textarea as HTMLTextAreaElement).value).toContain('LIVE: {{title}}');
  });

  it('saves content edits back to the selected template', async () => {
    await selectRow(0);

    const textarea = fixture.nativeElement.querySelector(
      'app-input textarea',
    ) as HTMLTextAreaElement;
    textarea.value = 'Updated content';
    textarea.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    editor()!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    await fixture.whenStable();

    expect(state().templates().find((t) => t.id === 1)?.content).toBe(
      'Updated content',
    );
  });

  it('deletes the selected template and hides the editor', async () => {
    await selectRow(0);

    buttonByText('Delete').click();
    fixture.detectChanges();

    expect(rows()).toHaveLength(2);
    expect(editor()).toBeNull();
  });

  it('creates a new template and opens it in the editor', async () => {
    buttonByText('New Template').click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(rows()).toHaveLength(4);
    expect(editor()).not.toBeNull();
    const inputs = fixture.nativeElement.querySelectorAll('app-input input');
    expect((inputs[1] as HTMLInputElement).value).toBe('New template');
  });
});

