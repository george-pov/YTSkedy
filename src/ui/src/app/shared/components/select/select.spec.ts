import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form, required } from '@angular/forms/signals';
import { beforeEach, describe, expect, it } from 'vitest';

import { Select, SelectOption } from './select';

const timeZoneOptions: SelectOption[] = [
  { value: 'UTC', label: 'UTC' },
  { value: 'Europe/London', label: 'London' },
];

@Component({
  selector: 'app-select-host',
  imports: [Select],
  template: `<app-select [field]="form.zone" label="Time zone" [options]="options" />`,
})
class SelectHost {
  readonly options = timeZoneOptions;
  readonly model = signal({ zone: '' });
  readonly form = form(this.model, (path) =>
    required(path.zone, { message: 'Time zone is required.' }),
  );
}

describe('Select', () => {
  let fixture: ComponentFixture<SelectHost>;
  let host: SelectHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(SelectHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders the label and a select control', () => {
    expect(
      fixture.nativeElement.querySelector('mat-label')?.textContent?.trim(),
    ).toBe('Time zone');
    expect(fixture.nativeElement.querySelector('mat-select')).not.toBeNull();
  });

  it('shows the field error once the field is touched', async () => {
    host.form.zone().markAsTouched();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(
      fixture.nativeElement.querySelector('mat-error')?.textContent?.trim(),
    ).toBe('Time zone is required.');
  });

  it('propagates the chosen option into the field value', async () => {
    const trigger = fixture.nativeElement.querySelector(
      '.mat-mdc-select-trigger',
    ) as HTMLElement;
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const london = Array.from(document.querySelectorAll('mat-option')).find(
      (option) => option.textContent?.includes('London'),
    ) as HTMLElement | undefined;
    london?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(host.model().zone).toBe('Europe/London');
  });
});
