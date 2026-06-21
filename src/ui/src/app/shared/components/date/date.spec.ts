import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form, required } from '@angular/forms/signals';
import { beforeEach, describe, expect, it } from 'vitest';

import { DateField } from './date';

@Component({
  selector: 'app-date-host',
  imports: [DateField],
  template: `<app-date [field]="form.date" label="Start date" />`,
})
class DateHost {
  readonly model = signal({ date: '' });
  readonly form = form(this.model, (path) =>
    required(path.date, { message: 'Start date is required.' }),
  );
}

describe('DateField', () => {
  let fixture: ComponentFixture<DateHost>;
  let host: DateHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(DateHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders the label and a native date input', () => {
    expect(
      fixture.nativeElement.querySelector('mat-label')?.textContent?.trim(),
    ).toBe('Start date');
    expect(
      fixture.nativeElement.querySelector('input')?.getAttribute('type'),
    ).toBe('date');
  });

  it('propagates input changes into the field value', async () => {
    const input = fixture.nativeElement.querySelector(
      'input',
    ) as HTMLInputElement;
    input.value = '2026-07-01';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(host.model().date).toBe('2026-07-01');
  });

  it('shows the field error once the field is touched', async () => {
    expect(fixture.nativeElement.querySelector('mat-error')).toBeNull();

    host.form.date().markAsTouched();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(
      fixture.nativeElement.querySelector('mat-error')?.textContent?.trim(),
    ).toBe('Start date is required.');
  });
});
