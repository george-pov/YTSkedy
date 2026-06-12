import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { beforeEach, describe, expect, it } from 'vitest';

import { TimeField } from './time';

function createField(
  control: FormControl<string>,
  inputs: { label?: string; errorMessages?: Record<string, string> } = {},
): ComponentFixture<TimeField> {
  const fixture = TestBed.createComponent(TimeField);
  fixture.componentRef.setInput('control', control);
  fixture.componentRef.setInput('label', inputs.label ?? '');
  fixture.componentRef.setInput('errorMessages', inputs.errorMessages ?? {});
  fixture.detectChanges();
  return fixture;
}

describe('TimeField', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
  });

  it('renders the label and a native time input', () => {
    const control = new FormControl('', { nonNullable: true });
    const fixture = createField(control, { label: 'Start time' });

    const label = fixture.nativeElement.querySelector('mat-label');
    const input = fixture.nativeElement.querySelector('input');

    expect(label?.textContent?.trim()).toBe('Start time');
    expect(input?.getAttribute('type')).toBe('time');
  });

  it('reflects the control value into the input', () => {
    const control = new FormControl('', { nonNullable: true });
    const fixture = createField(control);

    control.setValue('10:00');
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input');
    expect(input.value).toBe('10:00');
  });

  it('propagates input changes back to the control', () => {
    const control = new FormControl('', { nonNullable: true });
    const fixture = createField(control);

    const input = fixture.nativeElement.querySelector('input');
    input.value = '14:30';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(control.value).toBe('14:30');
  });

  it('shows the mapped error message when the control is invalid and touched', () => {
    const control = new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    });
    const fixture = createField(control, {
      errorMessages: { required: 'Time is required' },
    });

    control.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('mat-error');
    expect(error?.textContent?.trim()).toBe('Time is required');
  });

  it('shows no error message when the control is valid', () => {
    const control = new FormControl('10:00', {
      nonNullable: true,
      validators: [Validators.required],
    });
    const fixture = createField(control, {
      errorMessages: { required: 'Time is required' },
    });

    const error = fixture.nativeElement.querySelector('mat-error');
    expect(error).toBeNull();
  });
});
