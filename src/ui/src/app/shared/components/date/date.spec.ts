import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { beforeEach, describe, expect, it } from 'vitest';

import { DateField } from './date';

function createField(
  control: FormControl<string>,
  inputs: { label?: string; errorMessages?: Record<string, string> } = {},
): ComponentFixture<DateField> {
  const fixture = TestBed.createComponent(DateField);
  fixture.componentRef.setInput('control', control);
  fixture.componentRef.setInput('label', inputs.label ?? '');
  fixture.componentRef.setInput('errorMessages', inputs.errorMessages ?? {});
  fixture.detectChanges();
  return fixture;
}

describe('DateField', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
  });

  it('renders the label and a native date input', () => {
    const control = new FormControl('', { nonNullable: true });
    const fixture = createField(control, { label: 'Start date' });

    const label = fixture.nativeElement.querySelector('mat-label');
    const input = fixture.nativeElement.querySelector('input');

    expect(label?.textContent?.trim()).toBe('Start date');
    expect(input?.getAttribute('type')).toBe('date');
  });

  it('reflects the control value into the input', () => {
    const control = new FormControl('', { nonNullable: true });
    const fixture = createField(control);

    control.setValue('2026-06-06');
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input');
    expect(input.value).toBe('2026-06-06');
  });

  it('propagates input changes back to the control', () => {
    const control = new FormControl('', { nonNullable: true });
    const fixture = createField(control);

    const input = fixture.nativeElement.querySelector('input');
    input.value = '2026-07-01';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(control.value).toBe('2026-07-01');
  });

  it('shows the mapped error message when the control is invalid and touched', () => {
    const control = new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    });
    const fixture = createField(control, {
      errorMessages: { required: 'Date is required' },
    });

    control.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('mat-error');
    expect(error?.textContent?.trim()).toBe('Date is required');
  });

  it('shows no error message when the control is valid', () => {
    const control = new FormControl('2026-06-06', {
      nonNullable: true,
      validators: [Validators.required],
    });
    const fixture = createField(control, {
      errorMessages: { required: 'Date is required' },
    });

    const error = fixture.nativeElement.querySelector('mat-error');
    expect(error).toBeNull();
  });
});
