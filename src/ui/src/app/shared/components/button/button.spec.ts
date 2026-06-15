import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

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
    >
      {{ label() }}
    </app-button>
  `,
})
class ButtonHost {
  readonly icon = signal<string | undefined>(undefined);
  readonly iconButton = signal(false);
  readonly ariaLabel = signal<string | undefined>(undefined);
  readonly disabled = signal(false);
  readonly label = signal('Save');
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
    expect(fixture.nativeElement.querySelector('mat-icon')).toBeNull();
    expect(buttonEl(fixture).classList).not.toContain('mat-mdc-icon-button');
  });

  it('renders a leading icon alongside the label when icon is set', () => {
    host.icon.set('save');
    fixture.detectChanges();

    const icon = fixture.nativeElement.querySelector('mat-icon');
    expect(icon?.textContent).toBe('save');
    expect(buttonEl(fixture).textContent).toContain('Save');
    expect(buttonEl(fixture).classList).not.toContain('mat-mdc-icon-button');
  });

  it('renders a compact icon-only button named by the aria label', () => {
    host.iconButton.set(true);
    host.icon.set('edit');
    host.ariaLabel.set('Edit');
    fixture.detectChanges();

    const button = buttonEl(fixture);
    expect(button.classList).toContain('mat-mdc-icon-button');
    expect(button.getAttribute('aria-label')).toBe('Edit');
    expect(fixture.nativeElement.querySelector('mat-icon')?.textContent).toBe(
      'edit',
    );
  });

  it('hides the icon glyph from assistive technology', () => {
    host.icon.set('edit');
    fixture.detectChanges();

    expect(
      fixture.nativeElement
        .querySelector('mat-icon')
        ?.getAttribute('aria-hidden'),
    ).toBe('true');
  });

  it('reflects the disabled state on the native button', () => {
    host.disabled.set(true);
    fixture.detectChanges();

    expect(buttonEl(fixture).disabled).toBe(true);
  });
});
