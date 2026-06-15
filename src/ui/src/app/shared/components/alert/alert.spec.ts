import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { Alert, AlertVariant } from './alert';

@Component({
  selector: 'app-alert-host',
  imports: [Alert],
  template: `
    <app-alert
      [variant]="variant()"
      [dismissible]="dismissible()"
      (dismissed)="dismissedCount = dismissedCount + 1"
    >
      Something happened
    </app-alert>
  `,
})
class AlertHost {
  readonly variant = signal<AlertVariant>('info');
  readonly dismissible = signal(false);
  dismissedCount = 0;
}

function alertEl(fixture: ComponentFixture<AlertHost>): HTMLElement {
  return fixture.nativeElement.querySelector('app-alert') as HTMLElement;
}

describe('Alert', () => {
  let fixture: ComponentFixture<AlertHost>;
  let host: AlertHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(AlertHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('projects the message content', () => {
    expect(alertEl(fixture).textContent).toContain('Something happened');
  });

  it('announces errors and warnings assertively with role="alert"', () => {
    host.variant.set('error');
    fixture.detectChanges();
    expect(alertEl(fixture).getAttribute('role')).toBe('alert');

    host.variant.set('warning');
    fixture.detectChanges();
    expect(alertEl(fixture).getAttribute('role')).toBe('alert');
  });

  it('announces success and info politely with role="status"', () => {
    host.variant.set('success');
    fixture.detectChanges();
    expect(alertEl(fixture).getAttribute('role')).toBe('status');

    host.variant.set('info');
    fixture.detectChanges();
    expect(alertEl(fixture).getAttribute('role')).toBe('status');
  });

  it('applies a variant class', () => {
    host.variant.set('error');
    fixture.detectChanges();
    expect(alertEl(fixture).classList).toContain('error');
  });

  it('does not render a dismiss control by default', () => {
    expect(alertEl(fixture).querySelector('button')).toBeNull();
  });

  it('renders a labelled dismiss control when dismissible', () => {
    host.dismissible.set(true);
    fixture.detectChanges();

    const button = alertEl(fixture).querySelector('button');
    expect(button).not.toBeNull();
    expect(button?.getAttribute('aria-label')).toBe('Dismiss');
  });

  it('emits dismissed when the close control is activated', () => {
    host.dismissible.set(true);
    fixture.detectChanges();

    (alertEl(fixture).querySelector('button') as HTMLButtonElement).click();

    expect(host.dismissedCount).toBe(1);
  });
});
