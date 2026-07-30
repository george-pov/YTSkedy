import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatButton } from '@angular/material/button';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it } from 'vitest';

import { type IconName } from 'src/app/shared/components/icon/icon';
import { Button, type ButtonAppearance, type ButtonVariant } from './button';

@Component({
  selector: 'app-button-host',
  imports: [Button],
  template: `
    <app-button
      [icon]="icon()"
      [variant]="variant()"
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
  readonly variant = signal<ButtonVariant>('filled');
  readonly ariaLabel = signal<string | undefined>(undefined);
  readonly disabled = signal(false);
  readonly label = signal('Save');
  clickCount = 0;
}

function buttonEl(fixture: ComponentFixture<ButtonHost>): HTMLButtonElement {
  return fixture.nativeElement.querySelector('button') as HTMLButtonElement;
}

function buttonComponent(fixture: ComponentFixture<ButtonHost>): Button {
  return fixture.debugElement.query(By.directive(Button)).componentInstance as Button;
}

function materialButton(fixture: ComponentFixture<ButtonHost>): MatButton {
  return fixture.debugElement.query(By.directive(MatButton)).injector.get(MatButton);
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

  it('renders a filled button with no icon by default', () => {
    const button = buttonEl(fixture);

    expect(button.textContent).toContain('Save');
    expect(buttonComponent(fixture).variant()).toBe('filled');
    expect(button.classList).not.toContain('is-danger');
    expect(fixture.nativeElement.querySelector('app-icon')).toBeNull();
  });

  it.each([
    { variant: 'text', materialAppearance: 'text', danger: false },
    { variant: 'filled', materialAppearance: 'filled', danger: false },
    { variant: 'elevated', materialAppearance: 'elevated', danger: false },
    { variant: 'outlined', materialAppearance: 'outlined', danger: false },
    { variant: 'tonal', materialAppearance: 'tonal', danger: false },
    { variant: 'danger-filled', materialAppearance: 'filled', danger: true },
  ] satisfies readonly {
    variant: ButtonVariant;
    materialAppearance: ButtonAppearance;
    danger: boolean;
  }[])(
    'maps $variant to Material $materialAppearance with danger styling: $danger',
    ({ variant, materialAppearance, danger }) => {
      host.variant.set(variant);
      fixture.detectChanges();

      expect(materialButton(fixture).appearance).toBe(materialAppearance);
      expect(buttonEl(fixture).classList.contains('is-danger')).toBe(danger);
    },
  );

  it('renders a leading icon alongside the label when icon is set', () => {
    host.icon.set('save');
    fixture.detectChanges();

    const icon = fixture.nativeElement.querySelector('app-icon');
    expect(icon?.querySelector('mat-icon')?.textContent?.trim()).toBe('save');
    expect(icon?.classList).toContain('button-icon');
    expect(getComputedStyle(icon).marginRight).toBe('0.25rem');
    expect(buttonEl(fixture).textContent).toContain('Save');
  });

  it('renders a compact icon-only button named by the aria label', () => {
    host.variant.set('icon');
    host.icon.set('edit');
    host.ariaLabel.set('Edit');
    fixture.detectChanges();

    const button = buttonEl(fixture);
    expect(button.getAttribute('aria-label')).toBe('Edit');
    const icon = fixture.nativeElement.querySelector('app-icon');
    expect(icon?.querySelector('mat-icon')?.textContent?.trim()).toBe('edit');
    expect(icon?.classList).not.toContain('button-icon');

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
    host.variant.set('danger-filled');
    fixture.detectChanges();

    const button = buttonEl(fixture);

    expect(button.classList).toContain('is-danger');
    expect(button.disabled).toBe(true);
  });
});
