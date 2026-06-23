import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { beforeEach, describe, expect, it } from 'vitest';

import { YouTubeSettings } from './youtube-settings';

interface YouTubeSettingsModel {
  credentials: string;
  privacyStatus: string;
  madeForKids: string;
}

@Component({
  selector: 'app-youtube-settings-host',
  imports: [YouTubeSettings],
  template: `<app-youtube-settings
    [credentials]="form.credentials"
    [privacyStatus]="form.privacyStatus"
    [madeForKids]="form.madeForKids"
  />`,
})
class YouTubeSettingsHost {
  readonly model = signal<YouTubeSettingsModel>({
    credentials: 'main-youtube-channel',
    privacyStatus: 'private',
    madeForKids: 'false',
  });
  readonly form = form(this.model, () => {});
}

describe('YouTubeSettings', () => {
  let fixture: ComponentFixture<YouTubeSettingsHost>;
  let host: YouTubeSettingsHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(YouTubeSettingsHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders the credentials input and the two settings selects', () => {
    expect(
      fixture.nativeElement.querySelector('app-input input'),
    ).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('app-select')).toHaveLength(2);
  });

  it('binds the supplied credentials field to its model value', async () => {
    const input = fixture.nativeElement.querySelector(
      'app-input input',
    ) as HTMLInputElement;
    expect(input.value).toBe('main-youtube-channel');

    input.value = 'second-channel';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(host.model().credentials).toBe('second-channel');
  });
});
