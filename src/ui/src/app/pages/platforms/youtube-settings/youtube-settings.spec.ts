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
    clientSecretDisplayValue="*********A3B"
    refreshTokenDisplayValue="*********Z9Y"
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

  it('shows display values inside replacement inputs while values stay empty', () => {
    const inputs = Array.from(
      fixture.nativeElement.querySelectorAll('app-input input'),
    ) as HTMLInputElement[];

    expect(fixture.nativeElement.querySelectorAll('.secret-status')).toHaveLength(0);
    expect(inputs[1].placeholder).toBe('*********A3B');
    expect(inputs[2].placeholder).toBe('*********Z9Y');
    expect(inputs.some((input) => input.value === '*********A3B')).toBe(false);
    expect(inputs.some((input) => input.value === '*********Z9Y')).toBe(false);
    expect(host.model().clientSecret).toBe('');
    expect(host.model().refreshToken).toBe('');
  });

  it('hides a display value while focused and restores it when left blank', () => {
    const inputs = Array.from(
      fixture.nativeElement.querySelectorAll('app-input input'),
    ) as HTMLInputElement[];
    const clientSecret = inputs[1];

    clientSecret.dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    expect(clientSecret.value).toBe('');
    expect(clientSecret.placeholder).toBe('');
    expect(host.model().clientSecret).toBe('');

    clientSecret.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(clientSecret.value).toBe('');
    expect(clientSecret.placeholder).toBe('*********A3B');
    expect(host.model().clientSecret).toBe('');
  });

  it('keeps a replacement value after typing over the hidden display value', async () => {
    const inputs = Array.from(
      fixture.nativeElement.querySelectorAll('app-input input'),
    ) as HTMLInputElement[];
    const clientSecret = inputs[1];

    clientSecret.dispatchEvent(new Event('focus'));
    fixture.detectChanges();
    clientSecret.value = 'replacement-secret';
    clientSecret.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    clientSecret.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(clientSecret.value).toBe('replacement-secret');
    expect(host.model().clientSecret).toBe('replacement-secret');
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
