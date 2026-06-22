import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form, maxLength, required } from '@angular/forms/signals';
import { beforeEach, describe, expect, it } from 'vitest';

import { Input } from './input';

interface TitleModel {
  title: string;
}

@Component({
  selector: 'app-input-host',
  imports: [Input],
  template: `<app-input
    [field]="form.title"
    label="Title"
    [multiline]="multiline()"
    [showCharacterCount]="showCharacterCount()"
  />`,
})
class InputHost {
  readonly multiline = signal(false);
  readonly showCharacterCount = signal(false);
  readonly model = signal<TitleModel>({ title: '' });
  readonly form = form(this.model, (path) => {
    required(path.title, { message: 'Title is required.' });
    maxLength(path.title, 100, {
      message: 'Title must be 100 characters or fewer.',
    });
  });
}

describe('Input (signal forms field)', () => {
  let fixture: ComponentFixture<InputHost>;
  let host: InputHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(InputHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  function errorText(): string | null {
    const error = fixture.nativeElement.querySelector('mat-error');
    return error ? (error.textContent?.trim() ?? null) : null;
  }

  function counterText(): string | null {
    const hint = fixture.nativeElement.querySelector('mat-hint');
    return hint ? (hint.textContent?.trim() ?? null) : null;
  }

  it('renders the label and a native input', () => {
    expect(
      fixture.nativeElement.querySelector('mat-label')?.textContent?.trim(),
    ).toBe('Title');
    expect(fixture.nativeElement.querySelector('input')).not.toBeNull();
  });

  it('hides the error until the field is touched', () => {
    expect(errorText()).toBeNull();
  });

  it('shows the first error message once the field is touched', async () => {
    host.form.title().markAsTouched();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(errorText()).toBe('Title is required.');
  });

  it('propagates input changes back to the field value', async () => {
    const input = fixture.nativeElement.querySelector(
      'input',
    ) as HTMLInputElement;
    input.value = 'Hello';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(host.model().title).toBe('Hello');
  });

  it('renders a single-line input by default and a textarea when multiline', () => {
    expect(fixture.nativeElement.querySelector('input')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('textarea')).toBeNull();

    host.multiline.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('input')).toBeNull();
    expect(fixture.nativeElement.querySelector('textarea')).not.toBeNull();
  });

  it('caps the input length from the schema max length', async () => {
    await fixture.whenStable();

    const input = fixture.nativeElement.querySelector(
      'input',
    ) as HTMLInputElement;
    expect(input.maxLength).toBe(100);
  });

  it('hides the character counter by default even when the field has a max length', () => {
    expect(counterText()).toBeNull();
  });

  it('shows a used/max counter from the schema max when the counter is enabled', async () => {
    host.model.set({ title: 'Hello' });
    host.showCharacterCount.set(true);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(counterText()).toBe('5 / 100');
  });

  it('recalculates the character counter as the user types', async () => {
    host.showCharacterCount.set(true);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(counterText()).toBe('0 / 100');

    const input = fixture.nativeElement.querySelector(
      'input',
    ) as HTMLInputElement;
    input.value = 'Hi there';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(counterText()).toBe('8 / 100');
  });
});
