import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form, required } from '@angular/forms/signals';
import { beforeEach, describe, expect, it } from 'vitest';

import { TimeField } from './time';

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
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(TimeHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders the label and a native time input', () => {
    expect(
      fixture.nativeElement.querySelector('mat-label')?.textContent?.trim(),
    ).toBe('Start time');
    expect(
      fixture.nativeElement.querySelector('input')?.getAttribute('type'),
    ).toBe('time');
  });

  it('propagates input changes into the field value', async () => {
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
