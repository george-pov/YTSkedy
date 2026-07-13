import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { type IconName } from 'src/app/shared/components/icon/icon';
import { Button } from './button';

@Component({
  selector: 'app-button-host',
  imports: [Button],
  template: `
    <app-button
      [icon]="icon()"
      [iconButton]="iconButton()"
      [ariaLabel]="ariaLabel()"
      [disabled]="disabled()"
      (click)="clickCount = clickCount + 1"
    >
      {{ label() }}
    </app-button>
  `,
})
class ButtonHost {
  readonly icon = signal<IconName | undefined>(undefined);
  readonly iconButton = signal(false);
  readonly ariaLabel = signal<string | undefined>(undefined);
  readonly disabled = signal(false);
  readonly label = signal('Save');
  clickCount = 0;
}

function buttonEl(fixture: ComponentFixture<ButtonHost>): HTMLButtonElement {
  return fixture.nativeElement.querySelector('button') as HTMLButtonElement;
}

describe('Button', () => {
  let fixture: ComponentFixture<ButtonHost>;
  let host: ButtonHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(ButtonHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders a text button with no icon by default', () => {
    expect(buttonEl(fixture).textContent).toContain('Save');
    expect(fixture.nativeElement.querySelector('app-icon')).toBeNull();
  });

  it('renders a leading icon alongside the label when icon is set', () => {
    host.icon.set('save');
    fixture.detectChanges();

    const icon = fixture.nativeElement.querySelector('app-icon');
    expect(icon?.querySelector('svg')).not.toBeNull();
    expect(buttonEl(fixture).textContent).toContain('Save');
  });

  it('renders a compact icon-only button named by the aria label', () => {
    host.iconButton.set(true);
    host.icon.set('edit');
    host.ariaLabel.set('Edit');
    fixture.detectChanges();

    const button = buttonEl(fixture);
    expect(button.getAttribute('aria-label')).toBe('Edit');
    expect(button.textContent?.trim()).toBe('');
    expect(fixture.nativeElement.querySelector('app-icon svg')).not.toBeNull();

    button.click();
    expect(host.clickCount).toBe(1);
  });

  it('hides the icon glyph from assistive technology', () => {
    host.icon.set('edit');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-icon')?.getAttribute('aria-hidden')).toBe(
      'true',
    );
  });

  it('reflects the disabled state on the native button', () => {
    host.disabled.set(true);
    fixture.detectChanges();

    expect(buttonEl(fixture).disabled).toBe(true);
  });
});
