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
    [applicationPasswordConfigured]="true"
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
    expect(fixture.nativeElement.querySelectorAll('app-input input')).toHaveLength(3);
    expect(fixture.nativeElement.querySelectorAll('app-select')).toHaveLength(1);
  });

  it('renders read-only password status while the replacement input stays empty', () => {
    const text = fixture.nativeElement.textContent as string;
    const inputs = Array.from(
      fixture.nativeElement.querySelectorAll('app-input input'),
    ) as HTMLInputElement[];

    expect(text).toContain('*******');
    expect(inputs.some((input) => input.value === '*******')).toBe(false);
    expect(host.model().applicationPassword).toBe('');
  });
});
