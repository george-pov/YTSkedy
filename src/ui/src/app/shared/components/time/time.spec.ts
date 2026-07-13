import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { disabled, form, required } from '@angular/forms/signals';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { MatDateFormats } from '@angular/material/core';
import { MatFormFieldHarness } from '@angular/material/form-field/testing';
import {
  MatTimepickerInputHarness,
  MatTimepickerToggleHarness,
} from '@angular/material/timepicker/testing';
import { provideLuxonDateAdapter } from '@angular/material-luxon-adapter';
import { beforeEach, describe, expect, it } from 'vitest';

import { DATE_INPUT_FORMAT, TIME_INPUT_FORMAT } from 'src/app/shared/date-time/date-time-format';
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
  let loader: HarnessLoader;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideLuxonDateAdapter(testDateFormats)],
    });
    fixture = TestBed.createComponent(TimeHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
    loader = TestbedHarnessEnvironment.loader(fixture);
  });

  it('renders the label and timepicker controls', async () => {
    const formField = await loader.getHarness(MatFormFieldHarness);
    const input = await loader.getHarness(MatTimepickerInputHarness);
    const toggle = await loader.getHarness(MatTimepickerToggleHarness);

    expect(await formField.getLabel()).toBe('Start time');
    expect(await formField.getTextHints()).toEqual(['HH:mm']);
    expect(await input.isDisabled()).toBe(false);
    expect(await toggle.isDisabled()).toBe(false);
  });

  it('converts typed time changes into the string field value', async () => {
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = '14:30';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(host.model().time).toBe('14:30');
  });

  it('allows a time to be typed one character at a time', async () => {
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
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
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = '14:30 PM';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await fixture.whenStable();

    expect(host.model().time).toBe('14:30');
    expect(input.value).toBe('14:30');
  });

  it('does not commit a typed time outside the HH:mm format', async () => {
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = '4:30';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await fixture.whenStable();

    expect(host.model().time).toBe('');
  });

  it('shows the field error once the field is touched', async () => {
    const formField = await loader.getHarness(MatFormFieldHarness);
    expect(await formField.hasErrors()).toBe(false);

    host.form.time().markAsTouched();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(await formField.getTextErrors()).toEqual(['Start time is required.']);
  });

  it('disables the input and timepicker toggle when the field is disabled', async () => {
    host.disabled.set(true);
    fixture.detectChanges();
    await fixture.whenStable();

    const input = await loader.getHarness(MatTimepickerInputHarness);
    const toggle = await loader.getHarness(MatTimepickerToggleHarness);
    expect(await input.isDisabled()).toBe(true);
    expect(await toggle.isDisabled()).toBe(true);
  });
});
