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
}

@Component({
  selector: 'app-wordpress-settings-host',
  imports: [WordPressSettings],
  template: `<app-wordpress-settings
    [siteUrl]="form.siteUrl"
    [username]="form.username"
    [applicationPassword]="form.applicationPassword"
    [postStatus]="form.postStatus"
    passwordDisplayValue="*******"
  />`,
})
class WordPressSettingsHost {
  readonly model = signal<WordPressSettingsModel>({
    siteUrl: 'https://blog.example.test/',
    username: 'publisher',
    applicationPassword: '',
    postStatus: 'draft',
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
  });

  it('shows the display value inside the replacement input while the value stays empty', () => {
    const input = fixture.nativeElement.querySelector(
      'app-masked-input input',
    ) as HTMLInputElement;

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
