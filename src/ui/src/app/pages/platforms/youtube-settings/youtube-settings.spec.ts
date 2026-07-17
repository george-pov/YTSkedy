import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { form } from '@angular/forms/signals';
import { beforeEach, describe, expect, it } from 'vitest';

import { YouTubeSettings } from './youtube-settings';

interface YouTubeSettingsModel {
  clientId: string;
  clientSecret: string;
  refreshToken: string;
  privacyStatus: string;
  madeForKids: string;
  defaultAudioLanguage: string;
  defaultLanguage: string;
  categoryId: string;
  containsSyntheticMedia: string;
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
    [defaultAudioLanguage]="form.defaultAudioLanguage"
    [defaultLanguage]="form.defaultLanguage"
    [categoryId]="form.categoryId"
    [containsSyntheticMedia]="form.containsSyntheticMedia"
  />`,
})
class YouTubeSettingsHost {
  readonly model = signal<YouTubeSettingsModel>({
    clientId: 'client-id',
    clientSecret: '',
    refreshToken: '',
    privacyStatus: 'private',
    madeForKids: 'false',
    defaultAudioLanguage: '',
    defaultLanguage: '',
    categoryId: '',
    containsSyntheticMedia: 'false',
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

  it('renders the credential inputs and six settings selects', () => {
    expect(fixture.nativeElement.querySelectorAll('app-input input')).toHaveLength(1);
    expect(fixture.nativeElement.querySelectorAll('app-masked-input input')).toHaveLength(2);
    expect(fixture.nativeElement.querySelectorAll('app-select')).toHaveLength(6);
    expect(fixture.nativeElement.textContent).toContain('Stream language');
    expect(fixture.nativeElement.textContent).toContain('Title and description language');
    expect(fixture.nativeElement.textContent).toContain('Category');
    expect(fixture.nativeElement.textContent).toContain('Altered or synthetic content');
  });

  it('keeps an unknown stored category selectable and No before Yes', async () => {
    host.model.update((model) => ({ ...model, categoryId: '999' }));
    await fixture.whenStable();
    fixture.detectChanges();

    const selects = fixture.debugElement
      .queryAll(By.css('app-select'))
      .map((element) => element.componentInstance);
    const categoryOptions = selects[4].options();
    const syntheticMediaOptions = selects[5].options();

    expect(categoryOptions[0]).toEqual({ value: '', label: 'YouTube Default' });
    expect(categoryOptions.at(-1)).toEqual({ value: '999', label: 'Category #999' });
    expect(syntheticMediaOptions).toEqual([
      { value: 'false', label: 'No' },
      { value: 'true', label: 'Yes' },
    ]);
  });

  it('uses separate language catalogs and keeps unknown saved codes selectable', async () => {
    host.model.update((model) => ({
      ...model,
      defaultAudioLanguage: 'x-audio',
      defaultLanguage: 'x-metadata',
    }));
    await fixture.whenStable();
    fixture.detectChanges();

    const selects = fixture.debugElement
      .queryAll(By.css('app-select'))
      .map((element) => element.componentInstance);
    const audioOptions = selects[2].options();
    const metadataOptions = selects[3].options();

    expect(audioOptions.at(-1)).toEqual({
      value: 'x-audio',
      label: 'Language code: x-audio',
    });
    expect(metadataOptions.at(-1)).toEqual({
      value: 'x-metadata',
      label: 'Language code: x-metadata',
    });
    expect(audioOptions).toContainEqual({ value: 'zxx', label: 'Not applicable' });
    expect(metadataOptions.some((option: { value: string }) => option.value === 'zxx')).toBe(false);
  });

  it('shows display values inside replacement inputs while values stay empty', () => {
    const inputs = Array.from(
      fixture.nativeElement.querySelectorAll('app-masked-input input'),
    ) as HTMLInputElement[];

    expect(fixture.nativeElement.querySelectorAll('.secret-status')).toHaveLength(0);
    expect(inputs[0].value).toBe('*********A3B');
    expect(inputs[1].value).toBe('*********Z9Y');
    expect(host.model().clientSecret).toBe('');
    expect(host.model().refreshToken).toBe('');
  });

  it('hides a display value while focused and restores it when left blank', () => {
    const inputs = Array.from(
      fixture.nativeElement.querySelectorAll('app-masked-input input'),
    ) as HTMLInputElement[];
    const clientSecret = inputs[0];

    clientSecret.dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    expect(clientSecret.value).toBe('');
    expect(clientSecret.placeholder).toBe('');
    expect(host.model().clientSecret).toBe('');

    clientSecret.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(clientSecret.value).toBe('*********A3B');
    expect(host.model().clientSecret).toBe('');
  });

  it('masks a replacement value after typing over the hidden display value', async () => {
    const inputs = Array.from(
      fixture.nativeElement.querySelectorAll('app-masked-input input'),
    ) as HTMLInputElement[];
    const clientSecret = inputs[0];

    clientSecret.dispatchEvent(new Event('focus'));
    fixture.detectChanges();
    clientSecret.value = 'replacement-secret-N3W';
    clientSecret.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(clientSecret.value).toBe('replacement-secret-N3W');
    expect(host.model().clientSecret).toBe('replacement-secret-N3W');

    clientSecret.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(clientSecret.value).toBe('*********N3W');
    expect(host.model().clientSecret).toBe('replacement-secret-N3W');

    clientSecret.dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    expect(clientSecret.value).toBe('replacement-secret-N3W');
  });

  it('binds the supplied client ID field to its model value', async () => {
    const input = fixture.nativeElement.querySelector('app-input input') as HTMLInputElement;
    expect(input.value).toBe('client-id');

    input.value = 'second-client-id';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    expect(host.model().clientId).toBe('second-client-id');
  });
});
