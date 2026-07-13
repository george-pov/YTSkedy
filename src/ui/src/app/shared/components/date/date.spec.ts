import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { disabled, form, required } from '@angular/forms/signals';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { MatDateFormats } from '@angular/material/core';
import {
  MatDatepickerInputHarness,
  MatDatepickerToggleHarness,
} from '@angular/material/datepicker/testing';
import { MatFormFieldHarness } from '@angular/material/form-field/testing';
import { provideLuxonDateAdapter } from '@angular/material-luxon-adapter';
import { beforeEach, describe, expect, it } from 'vitest';

import {
  DATE_INPUT_DISPLAY_FORMAT,
  DATE_INPUT_FORMAT,
} from 'src/app/shared/date-time/date-time-format';
import { DateField } from './date';

const testDateFormats: MatDateFormats = {
  parse: { dateInput: DATE_INPUT_FORMAT },
  display: {
    dateInput: DATE_INPUT_DISPLAY_FORMAT,
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
  readonly disabled = signal(false);
  readonly model = signal({ date: '' });
  readonly form = form(this.model, (path) => {
    required(path.date, { message: 'Start date is required.' });
    disabled(path.date, { when: () => this.disabled() });
  });
}

describe('DateField', () => {
  let fixture: ComponentFixture<DateHost>;
  let host: DateHost;
  let loader: HarnessLoader;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideLuxonDateAdapter(testDateFormats)],
    });
    fixture = TestBed.createComponent(DateHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
    loader = TestbedHarnessEnvironment.loader(fixture);
  });

  it('renders the label and datepicker controls', async () => {
    const formField = await loader.getHarness(MatFormFieldHarness);
    const input = await loader.getHarness(MatDatepickerInputHarness);
    const toggle = await loader.getHarness(MatDatepickerToggleHarness);

    expect(await formField.getLabel()).toBe('Start date');
    expect(await formField.getTextHints()).toEqual(['YYYY-MM-DD']);
    expect(await input.hasCalendar()).toBe(true);
    expect(await toggle.isDisabled()).toBe(false);
  });

  it('converts typed date changes into the string field value', async () => {
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = '2026-07-01';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(host.model().date).toBe('2026-07-01');
  });

  it('displays the selected date with its weekday while retaining the ISO field value', async () => {
    host.form.date().value.set('2026-07-31');
    fixture.detectChanges();
    await fixture.whenStable();

    expect((fixture.nativeElement.querySelector('input') as HTMLInputElement).value).toBe(
      '2026-07-31 (Friday)',
    );
    expect(host.model().date).toBe('2026-07-31');
  });

  it('allows a date to be typed one character at a time', async () => {
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.focus();

    for (const character of '2026-07-31') {
      input.value += character;
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
      await fixture.whenStable();
    }

    expect(host.model().date).toBe('2026-07-31');
  });

  it('ignores non-format text in typed date input', async () => {
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = '2026-07-31 Sunday';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await fixture.whenStable();

    expect(host.model().date).toBe('2026-07-31');
    expect(input.value).toBe('2026-07-31 (Friday)');
  });

  it('does not commit a typed date outside the YYYY-MM-DD format', async () => {
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = '2026-7-31';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await fixture.whenStable();

    expect(host.model().date).toBe('');
  });

  it('shows the field error once the field is touched', async () => {
    const formField = await loader.getHarness(MatFormFieldHarness);
    expect(await formField.hasErrors()).toBe(false);

    host.form.date().markAsTouched();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(await formField.getTextErrors()).toEqual(['Start date is required.']);
  });

  it('disables the input and datepicker toggle when the field is disabled', async () => {
    host.disabled.set(true);
    fixture.detectChanges();
    await fixture.whenStable();

    const input = await loader.getHarness(MatDatepickerInputHarness);
    const toggle = await loader.getHarness(MatDatepickerToggleHarness);
    expect(await input.isDisabled()).toBe(true);
    expect(await toggle.isDisabled()).toBe(true);
  });
});
