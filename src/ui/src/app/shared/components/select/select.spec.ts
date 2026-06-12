import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { beforeEach, describe, expect, it } from 'vitest';

import { Select, SelectOption } from './select';

const timeZoneOptions: SelectOption[] = [
  { value: 'UTC', label: 'UTC' },
  { value: 'Europe/London', label: 'London' },
];

function createSelect(
  control: FormControl<string>,
  inputs: {
    label?: string;
    options?: SelectOption[];
    required?: boolean;
    errorMessages?: Record<string, string>;
  } = {},
): ComponentFixture<Select> {
  const fixture = TestBed.createComponent(Select);
  fixture.componentRef.setInput('control', control);
  fixture.componentRef.setInput('label', inputs.label ?? '');
  fixture.componentRef.setInput('options', inputs.options ?? []);
  fixture.componentRef.setInput('required', inputs.required ?? false);
  fixture.componentRef.setInput('errorMessages', inputs.errorMessages ?? {});
  fixture.detectChanges();
  return fixture;
}

describe('Select', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
  });

  it('renders the label and a select control', () => {
    const control = new FormControl('', { nonNullable: true });
    const fixture = createSelect(control, {
      label: 'Time zone',
      options: timeZoneOptions,
    });

    const label = fixture.nativeElement.querySelector('mat-label');
    const select = fixture.nativeElement.querySelector('mat-select');

    expect(label?.textContent?.trim()).toBe('Time zone');
    expect(select).not.toBeNull();
  });

  it('marks the select as required when required is set', () => {
    const control = new FormControl('', { nonNullable: true });
    const fixture = createSelect(control, {
      options: timeZoneOptions,
      required: true,
    });

    const combobox = fixture.nativeElement.querySelector('[role="combobox"]');
    expect(combobox?.getAttribute('aria-required')).toBe('true');
  });

  it('propagates the chosen option back to the control', async () => {
    const control = new FormControl('', { nonNullable: true });
    const fixture = createSelect(control, { options: timeZoneOptions });

    const trigger = fixture.nativeElement.querySelector('.mat-mdc-select-trigger');
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const options = document.querySelectorAll('mat-option');
    const londonOption = Array.from(options).find((option) =>
      option.textContent?.includes('London'),
    ) as HTMLElement | undefined;
    londonOption?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(control.value).toBe('Europe/London');
  });

  it('shows the mapped error message when the control is invalid and touched', () => {
    const control = new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    });
    const fixture = createSelect(control, {
      options: timeZoneOptions,
      errorMessages: { required: 'Time zone is required' },
    });

    control.markAsTouched();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('mat-error');
    expect(error?.textContent?.trim()).toBe('Time zone is required');
  });

  it('shows no error message when the control is valid', () => {
    const control = new FormControl('UTC', {
      nonNullable: true,
      validators: [Validators.required],
    });
    const fixture = createSelect(control, {
      options: timeZoneOptions,
      errorMessages: { required: 'Time zone is required' },
    });

    const error = fixture.nativeElement.querySelector('mat-error');
    expect(error).toBeNull();
  });
});
