import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form, required } from '@angular/forms/signals';
import { MatDateFormats } from '@angular/material/core';
import { provideLuxonDateAdapter } from '@angular/material-luxon-adapter';
import { beforeEach, describe, expect, it } from 'vitest';

import { DateField } from './date';

const testDateFormats: MatDateFormats = {
  parse: { dateInput: 'yyyy-MM-dd' },
  display: {
    dateInput: 'yyyy-MM-dd',
    monthYearLabel: 'LLL yyyy',
    dateA11yLabel: 'DDD',
    monthYearA11yLabel: 'LLLL yyyy',
  },
};

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
      providers: [
        provideZonelessChangeDetection(),
        provideLuxonDateAdapter(testDateFormats),
      ],
    });
    fixture = TestBed.createComponent(DateHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders the label and Material datepicker controls', () => {
    expect(
      fixture.nativeElement.querySelector('mat-label')?.textContent?.trim(),
    ).toBe('Start date');
    expect(
      fixture.nativeElement.querySelector('input[matInput]'),
    ).not.toBeNull();
    expect(
      fixture.nativeElement.querySelector('mat-datepicker-toggle'),
    ).not.toBeNull();
    expect(fixture.nativeElement.querySelector('mat-datepicker')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('mat-hint')?.textContent).toContain(
      'YYYY-MM-DD',
    );
  });

  it('converts typed date changes into the string field value', async () => {
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
