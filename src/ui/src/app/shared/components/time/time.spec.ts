import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form, required } from '@angular/forms/signals';
import { MatDateFormats } from '@angular/material/core';
import { provideLuxonDateAdapter } from '@angular/material-luxon-adapter';
import { beforeEach, describe, expect, it } from 'vitest';

import { TimeField } from './time';

const testDateFormats: MatDateFormats = {
  parse: { dateInput: 'yyyy-MM-dd', timeInput: 'HH:mm' },
  display: {
    dateInput: 'yyyy-MM-dd',
    monthYearLabel: 'LLL yyyy',
    dateA11yLabel: 'DDD',
    monthYearA11yLabel: 'LLLL yyyy',
    timeInput: 'HH:mm',
    timeOptionLabel: 'HH:mm',
  },
};

@Component({
  selector: 'app-time-host',
  imports: [TimeField],
  template: `<app-time [field]="form.time" label="Start time" />`,
})
class TimeHost {
  readonly model = signal({ time: '' });
  readonly form = form(this.model, (path) =>
    required(path.time, { message: 'Start time is required.' }),
  );
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

  it('shows the field error once the field is touched', async () => {
    expect(fixture.nativeElement.querySelector('mat-error')).toBeNull();

    host.form.time().markAsTouched();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(
      fixture.nativeElement.querySelector('mat-error')?.textContent?.trim(),
    ).toBe('Start time is required.');
  });
});
