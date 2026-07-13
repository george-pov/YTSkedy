import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { form, maxLength, required } from '@angular/forms/signals';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it } from 'vitest';

import { MaskedInput } from './masked-input';

interface SecretModel {
  secret: string;
}

type MaskedInputMode = 'secret' | 'password';

@Component({
  selector: 'app-masked-input-host',
  imports: [MaskedInput],
  template: `<app-masked-input
    [field]="form.secret"
    label="Secret"
    placeholder="Paste secret"
    [displayValue]="displayValue()"
    [maskMode]="maskMode()"
  />`,
})
class MaskedInputHost {
  readonly displayValue = signal('');
  readonly maskMode = signal<MaskedInputMode>('secret');
  readonly model = signal<SecretModel>({ secret: '' });
  readonly form = form(this.model, (path) => {
    required(path.secret, { message: 'Secret is required.' });
    maxLength(path.secret, 20, {
      message: 'Secret must be 20 characters or fewer.',
    });
  });
}

describe('MaskedInput', () => {
  let fixture: ComponentFixture<MaskedInputHost>;
  let host: MaskedInputHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(MaskedInputHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  function input(): HTMLInputElement {
    return fixture.nativeElement.querySelector('input') as HTMLInputElement;
  }

  it('renders the label and native input', () => {
    const maskedInput = fixture.debugElement.query(By.directive(MaskedInput))
      .componentInstance as MaskedInput;
    expect(maskedInput.label()).toBe('Secret');
    expect(input()).not.toBeNull();
  });

  it('shows the backend display value without writing it to the field', () => {
    host.displayValue.set('*********A3B');
    fixture.detectChanges();

    expect(input().value).toBe('*********A3B');
    expect(host.model().secret).toBe('');
  });

  it('hides a backend display value on focus and restores it on blank blur', () => {
    host.displayValue.set('*********A3B');
    fixture.detectChanges();

    input().dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    expect(input().value).toBe('');
    expect(host.model().secret).toBe('');

    input().dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(input().value).toBe('*********A3B');
    expect(host.model().secret).toBe('');
  });

  it('shows a replacement in clear text while focused and masks it on blur', async () => {
    input().dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    input().value = 'replacement-secret-A3B';
    input().dispatchEvent(new Event('input'));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(input().value).toBe('replacement-secret-A3B');
    expect(host.model().secret).toBe('replacement-secret-A3B');

    input().dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(input().value).toBe('*********A3B');
    expect(host.model().secret).toBe('replacement-secret-A3B');
  });

  it('restores a replacement value for editing on focus', async () => {
    input().dispatchEvent(new Event('focus'));
    input().value = 'replacement-secret-A3B';
    input().dispatchEvent(new Event('input'));
    await fixture.whenStable();
    input().dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    input().dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    expect(input().value).toBe('replacement-secret-A3B');
  });

  it('masks password values without a visible suffix', async () => {
    host.maskMode.set('password');
    fixture.detectChanges();

    input().dispatchEvent(new Event('focus'));
    input().value = 'application-password';
    input().dispatchEvent(new Event('input'));
    await fixture.whenStable();

    input().dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(input().value).toBe('*******');
    expect(host.model().secret).toBe('application-password');
  });

  it('caps the input length from the schema max length', () => {
    expect(input().maxLength).toBe(20);
  });

  it('shows the first error after blur touches the field', () => {
    expect(fixture.nativeElement.textContent).not.toContain('Secret is required.');

    input().dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Secret is required.');
  });
});
