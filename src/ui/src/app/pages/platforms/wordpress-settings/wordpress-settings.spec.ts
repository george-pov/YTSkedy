import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { beforeEach, describe, expect, it } from 'vitest';

import { WordPressSettings } from './wordpress-settings';

interface WordPressSettingsModel {
  siteUrl: string;
  username: string;
  applicationPassword: string;
  postStatus: string;
  sticky: boolean;
  scheduleOffsetHours: string;
}

@Component({
  selector: 'app-wordpress-settings-host',
  imports: [WordPressSettings],
  template: `<app-wordpress-settings
    [siteUrl]="form.siteUrl"
    [username]="form.username"
    [applicationPassword]="form.applicationPassword"
    [postStatus]="form.postStatus"
    [sticky]="form.sticky"
    [scheduleOffsetHours]="form.scheduleOffsetHours"
    passwordDisplayValue="*******"
  />`,
})
class WordPressSettingsHost {
  readonly model = signal<WordPressSettingsModel>({
    siteUrl: 'https://blog.example.test/',
    username: 'publisher',
    applicationPassword: '',
    postStatus: 'draft',
    sticky: false,
    scheduleOffsetHours: '',
  });
  readonly form = form(this.model, () => {});
}

describe('WordPressSettings', () => {
  let fixture: ComponentFixture<WordPressSettingsHost>;
  let host: WordPressSettingsHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(WordPressSettingsHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders the provider inputs and post status select', () => {
    expect(fixture.nativeElement.querySelectorAll('app-input input')).toHaveLength(2);
    expect(fixture.nativeElement.querySelectorAll('app-masked-input input')).toHaveLength(1);
    expect(fixture.nativeElement.querySelectorAll('app-select')).toHaveLength(1);
    expect(fixture.nativeElement.querySelectorAll('app-checkbox')).toHaveLength(1);
  });

  it('offers all five post statuses in display order', async () => {
    const trigger = fixture.nativeElement.querySelector('.mat-mdc-select-trigger') as HTMLElement;
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const labels = Array.from(document.querySelectorAll('mat-option')).map((option) =>
      option.textContent?.trim(),
    );

    expect(labels).toEqual(['Draft', 'Pending', 'Private', 'Scheduled', 'Publish']);

    const scheduled = Array.from(document.querySelectorAll('mat-option')).find(
      (option) => option.textContent?.trim() === 'Scheduled',
    ) as HTMLElement | undefined;
    scheduled?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(host.model().postStatus).toBe('future');
  });

  it('keeps the sticky checkbox visible and synchronizes checked state', async () => {
    const checkbox = fixture.nativeElement.querySelector(
      'app-checkbox input[type="checkbox"]',
    ) as HTMLInputElement;

    expect(checkbox.checked).toBe(false);
    expect(fixture.nativeElement.querySelector('app-checkbox')?.textContent).toContain(
      'Make this post sticky',
    );

    checkbox.click();
    await fixture.whenStable();

    expect(host.model().sticky).toBe(true);
  });

  it.each(['draft', 'pending', 'private', 'publish'])(
    'hides the scheduled offset for %s',
    (postStatus) => {
      host.model.update((model) => ({ ...model, postStatus }));
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('input[type="number"]')).toBeNull();
    },
  );

  it('shows the bounded whole-hours input for Scheduled', () => {
    host.model.update((model) => ({
      ...model,
      postStatus: 'future',
      scheduleOffsetHours: '24',
    }));
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input[type="number"]') as HTMLInputElement;
    expect(input).not.toBeNull();
    expect(input.value).toBe('24');
    expect(input.getAttribute('min')).toBe('1');
    expect(input.getAttribute('max')).toBe('168');
    expect(input.getAttribute('step')).toBe('1');
  });

  it('shows the display value inside the replacement input while the value stays empty', () => {
    const input = fixture.nativeElement.querySelector('app-masked-input input') as HTMLInputElement;

    expect(fixture.nativeElement.querySelectorAll('.secret-status')).toHaveLength(0);
    expect(input.value).toBe('*******');
    expect(host.model().applicationPassword).toBe('');
  });

  it('hides the display value while focused and restores it when left blank', () => {
    const applicationPassword = fixture.nativeElement.querySelector(
      'app-masked-input input',
    ) as HTMLInputElement;

    applicationPassword.dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    expect(applicationPassword.value).toBe('');
    expect(applicationPassword.placeholder).toBe('');
    expect(host.model().applicationPassword).toBe('');

    applicationPassword.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(applicationPassword.value).toBe('*******');
    expect(host.model().applicationPassword).toBe('');
  });

  it('masks a replacement password on blur while preserving the raw model value', async () => {
    const applicationPassword = fixture.nativeElement.querySelector(
      'app-masked-input input',
    ) as HTMLInputElement;

    applicationPassword.dispatchEvent(new Event('focus'));
    fixture.detectChanges();
    applicationPassword.value = 'replacement-password';
    applicationPassword.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(applicationPassword.value).toBe('replacement-password');
    expect(host.model().applicationPassword).toBe('replacement-password');

    applicationPassword.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(applicationPassword.value).toBe('*******');
    expect(host.model().applicationPassword).toBe('replacement-password');
  });
});
