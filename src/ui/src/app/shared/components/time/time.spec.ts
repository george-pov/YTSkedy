import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { disabled, form, required } from '@angular/forms/signals';
import { MatDateFormats } from '@angular/material/core';
import { provideLuxonDateAdapter } from '@angular/material-luxon-adapter';
import { beforeEach, describe, expect, it } from 'vitest';

import {
  DATE_INPUT_FORMAT,
  TIME_INPUT_FORMAT,
} from 'src/app/shared/date-time/date-time-format';
import { TimeField } from './time';

const testDateFormats: MatDateFormats = {
  parse: { dateInput: DATE_INPUT_FORMAT, timeInput: TIME_INPUT_FORMAT },
  display: {
    dateInput: DATE_INPUT_FORMAT,
    monthYearLabel: 'LLL yyyy',
    dateA11yLabel: 'DDD',
    monthYearA11yLabel: 'LLLL yyyy',
    timeInput: TIME_INPUT_FORMAT,
    timeOptionLabel: TIME_INPUT_FORMAT,
  },
};

@Component({
  selector: 'app-time-host',
  imports: [TimeField],
  template: `<app-time [field]="form.time" label="Start time" />`,
})
class TimeHost {
  readonly disabled = signal(false);
  readonly model = signal({ time: '' });
  readonly form = form(this.model, (path) => {
    required(path.time, { message: 'Start time is required.' });
    disabled(path.time, { when: () => this.disabled() });
  });
}

describe('TimeField', () => {
  let fixture: ComponentFixture<TimeHost>;
  let host: TimeHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideLuxonDateAdapter(testDateFormats),
      ],
    });
    fixture = TestBed.createComponent(TimeHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders the label and Material timepicker controls', () => {
    expect(
      fixture.nativeElement.querySelector('mat-label')?.textContent?.trim(),
    ).toBe('Start time');
    expect(
      fixture.nativeElement.querySelector('input[matInput]'),
    ).not.toBeNull();
    expect(
      fixture.nativeElement.querySelector('mat-timepicker-toggle'),
    ).not.toBeNull();
    expect(fixture.nativeElement.querySelector('mat-timepicker')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('mat-hint')?.textContent).toContain(
      'HH:mm',
    );
  });

  it('converts typed time changes into the string field value', async () => {
    const input = fixture.nativeElement.querySelector(
      'input',
    ) as HTMLInputElement;
    input.value = '14:30';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(host.model().time).toBe('14:30');
  });

  it('allows a time to be typed one character at a time', async () => {
    const input = fixture.nativeElement.querySelector(
      'input',
    ) as HTMLInputElement;
    input.focus();

    for (const character of '14:30') {
      input.value += character;
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
      await fixture.whenStable();
    }

    expect(input.value).toBe('14:30');
    expect(host.model().time).toBe('14:30');
  });

  it('ignores non-format text in typed time input', async () => {
    const input = fixture.nativeElement.querySelector(
      'input',
    ) as HTMLInputElement;
    input.value = '14:30 PM';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await fixture.whenStable();

    expect(host.model().time).toBe('14:30');
    expect(input.value).toBe('14:30');
  });

  it('does not commit a typed time outside the HH:mm format', async () => {
    const input = fixture.nativeElement.querySelector(
      'input',
    ) as HTMLInputElement;
    input.value = '4:30';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await fixture.whenStable();

    expect(host.model().time).toBe('');
  });

  it('shows the field error once the field is touched', async () => {
    expect(fixture.nativeElement.querySelector('mat-error')).toBeNull();

    host.form.time().markAsTouched();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(
      fixture.nativeElement.querySelector('mat-error')?.textContent?.trim(),
    ).toBe('Start time is required.');
  });

  it('disables the Material input and timepicker toggle when the field is disabled', async () => {
    host.disabled.set(true);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(
      (fixture.nativeElement.querySelector('input') as HTMLInputElement).disabled,
    ).toBe(true);
    expect(
      (fixture.nativeElement.querySelector('mat-timepicker-toggle button') as HTMLButtonElement)
        .disabled,
    ).toBe(true);
  });
});
