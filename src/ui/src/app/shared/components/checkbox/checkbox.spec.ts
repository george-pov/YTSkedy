import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { disabled, form } from '@angular/forms/signals';
import { beforeEach, describe, expect, it } from 'vitest';

import { Checkbox } from './checkbox';

interface CheckboxHostModel {
  selected: boolean;
}

@Component({
  selector: 'app-checkbox-host',
  imports: [Checkbox],
  template: `<app-checkbox [field]="form.selected" label="Make sticky" />`,
})
class CheckboxHost {
  readonly isDisabled = signal(false);
  readonly model = signal<CheckboxHostModel>({ selected: false });
  readonly form = form(this.model, (path) => {
    disabled(path.selected, { when: () => this.isDisabled() });
  });
}

describe('Checkbox (signal forms field)', () => {
  let fixture: ComponentFixture<CheckboxHost>;
  let host: CheckboxHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(CheckboxHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  function checkbox(): HTMLInputElement {
    return fixture.nativeElement.querySelector('input[type="checkbox"]') as HTMLInputElement;
  }

  it('renders an initial unchecked model state', () => {
    expect(checkbox().checked).toBe(false);
  });

  it('renders an initial checked model state', async () => {
    fixture.destroy();
    fixture = TestBed.createComponent(CheckboxHost);
    host = fixture.componentInstance;
    host.model.set({ selected: true });
    fixture.detectChanges();
    await fixture.whenStable();

    expect(checkbox().checked).toBe(true);
  });

  it('synchronizes a click to the model', async () => {
    checkbox().click();
    await fixture.whenStable();

    expect(host.model().selected).toBe(true);
  });

  it('synchronizes model changes to the control', async () => {
    host.model.set({ selected: true });
    fixture.detectChanges();
    await fixture.whenStable();

    expect(checkbox().checked).toBe(true);
  });

  it('renders the label', () => {
    expect(fixture.nativeElement.querySelector('mat-checkbox')?.textContent?.trim()).toBe(
      'Make sticky',
    );
  });

  it('binds Signal Forms disabled state to the control', async () => {
    host.isDisabled.set(true);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(checkbox().disabled).toBe(true);
  });
});
