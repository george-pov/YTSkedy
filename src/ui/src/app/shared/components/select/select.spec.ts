import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form, required } from '@angular/forms/signals';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { MatFormFieldHarness } from '@angular/material/form-field/testing';
import { MatSelectHarness } from '@angular/material/select/testing';
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
  let loader: HarnessLoader;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(SelectHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
    loader = TestbedHarnessEnvironment.loader(fixture);
  });

  it('renders the label and a select control', async () => {
    const formField = await loader.getHarness(MatFormFieldHarness);
    const select = await loader.getHarness(MatSelectHarness);

    expect(await formField.getLabel()).toBe('Time zone');
    expect(await select.isEmpty()).toBe(true);
  });

  it('shows the field error once the field is touched', async () => {
    host.form.zone().markAsTouched();
    fixture.detectChanges();
    await fixture.whenStable();

    const formField = await loader.getHarness(MatFormFieldHarness);
    expect(await formField.getTextErrors()).toEqual(['Time zone is required.']);
  });

  it('propagates the chosen option into the field value', async () => {
    const select = await loader.getHarness(MatSelectHarness);
    await select.open();
    await select.clickOptions({ text: 'London' });

    expect(host.model().zone).toBe('Europe/London');
  });
});
