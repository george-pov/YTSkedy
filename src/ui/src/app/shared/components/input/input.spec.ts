import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form, required } from '@angular/forms/signals';
import { beforeEach, describe, expect, it } from 'vitest';

import { Input } from './input';

interface TitleModel {
  title: string;
}

@Component({
  selector: 'app-input-host',
  imports: [Input],
  template: `<app-input [field]="form.title" label="Title" />`,
})
class InputHost {
  readonly model = signal<TitleModel>({ title: '' });
  readonly form = form(this.model, (path) =>
    required(path.title, { message: 'Title is required.' }),
  );
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
});
