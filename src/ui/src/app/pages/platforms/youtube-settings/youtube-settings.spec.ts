import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { beforeEach, describe, expect, it } from 'vitest';

import { YouTubeSettings } from './youtube-settings';

interface YouTubeSettingsModel {
  clientId: string;
  clientSecret: string;
  refreshToken: string;
  privacyStatus: string;
  madeForKids: string;
}

@Component({
  selector: 'app-youtube-settings-host',
  imports: [YouTubeSettings],
  template: `<app-youtube-settings
    [clientId]="form.clientId"
    [clientSecret]="form.clientSecret"
    [refreshToken]="form.refreshToken"
    [clientSecretConfigured]="true"
    [refreshTokenConfigured]="true"
    [privacyStatus]="form.privacyStatus"
    [madeForKids]="form.madeForKids"
  />`,
})
class YouTubeSettingsHost {
  readonly model = signal<YouTubeSettingsModel>({
    clientId: 'client-id',
    clientSecret: '',
    refreshToken: '',
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

  it('renders the credential inputs and the two settings selects', () => {
    expect(fixture.nativeElement.querySelectorAll('app-input input')).toHaveLength(3);
    expect(fixture.nativeElement.querySelectorAll('app-select')).toHaveLength(2);
  });

  it('binds the supplied client ID field to its model value', async () => {
    const input = fixture.nativeElement.querySelector(
      'app-input input',
    ) as HTMLInputElement;
    expect(input.value).toBe('client-id');

    input.value = 'second-client-id';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(host.model().clientId).toBe('second-client-id');
  });
});
