import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form, maxLength, required } from '@angular/forms/signals';
import { By } from '@angular/platform-browser';
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
    [placeholder]="placeholder()"
    [multiline]="multiline()"
    [showCharacterCount]="showCharacterCount()"
    [inputType]="inputType()"
    [min]="min()"
    [max]="max()"
    [step]="step()"
  />`,
})
class InputHost {
  readonly multiline = signal(false);
  readonly showCharacterCount = signal(false);
  readonly placeholder = signal('');
  readonly inputType = signal<'text' | 'number'>('text');
  readonly min = signal<number | undefined>(undefined);
  readonly max = signal<number | undefined>(undefined);
  readonly step = signal<number | undefined>(undefined);
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

  it('renders the label and a native input', () => {
    const input = fixture.debugElement.query(By.directive(Input)).componentInstance as Input;
    expect(input.label()).toBe('Title');
    expect(fixture.nativeElement.querySelector('input')).not.toBeNull();
  });

  it('renders text semantics without numeric attributes by default', () => {
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;

    expect(input.type).toBe('text');
    expect(input.hasAttribute('min')).toBe(false);
    expect(input.hasAttribute('max')).toBe(false);
    expect(input.hasAttribute('step')).toBe(false);
  });

  it('renders bounded numeric attributes and keeps the string field model', async () => {
    host.inputType.set('number');
    host.min.set(1);
    host.max.set(168);
    host.step.set(1);
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    expect(input.type).toBe('number');
    expect(input.getAttribute('min')).toBe('1');
    expect(input.getAttribute('max')).toBe('168');
    expect(input.getAttribute('step')).toBe('1');

    input.value = '24';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(host.model().title).toBe('24');
  });

  it('hides the error until the field is touched', () => {
    expect(fixture.nativeElement.textContent).not.toContain('Title is required.');
  });

  it('shows the first error message once the field is touched', async () => {
    host.form.title().markAsTouched();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Title is required.');
  });

  it('propagates input changes back to the field value', async () => {
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
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

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    expect(input.maxLength).toBe(100);
  });

  it('hides the character counter by default even when the field has a max length', () => {
    expect(fixture.nativeElement.textContent).not.toContain('0 / 100');
  });

  it('shows a used/max counter from the schema max when the counter is enabled', async () => {
    host.model.set({ title: 'Hello' });
    host.showCharacterCount.set(true);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('5 / 100');
  });

  it('recalculates the character counter as the user types', async () => {
    host.showCharacterCount.set(true);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('0 / 100');

    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = 'Hi there';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('8 / 100');
  });
});
