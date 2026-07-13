import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { ProgressBar, ProgressBarMode } from './progress-bar';

@Component({
  selector: 'app-progress-bar-host',
  imports: [ProgressBar],
  template: ` <app-progress-bar [mode]="mode()" [value]="value()" [label]="label()" /> `,
})
class ProgressBarHost {
  readonly mode = signal<ProgressBarMode>('indeterminate');
  readonly value = signal(0);
  readonly label = signal('');
}

function barEl(fixture: ComponentFixture<ProgressBarHost>): HTMLElement {
  return fixture.nativeElement.querySelector('[role="progressbar"]') as HTMLElement;
}

describe('ProgressBar', () => {
  let fixture: ComponentFixture<ProgressBarHost>;
  let host: ProgressBarHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(ProgressBarHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders a progress bar with the progressbar role', () => {
    const bar = barEl(fixture);
    expect(bar).not.toBeNull();
    expect(bar.getAttribute('role')).toBe('progressbar');
  });

  it('is indeterminate by default and exposes no current value', () => {
    expect(barEl(fixture).getAttribute('aria-valuenow')).toBeNull();
  });

  it('exposes the completion value in determinate mode', () => {
    host.mode.set('determinate');
    host.value.set(60);
    fixture.detectChanges();

    expect(barEl(fixture).getAttribute('aria-valuenow')).toBe('60');
  });

  it('names the progress bar with the accessible label', () => {
    host.label.set('Loading calendar events');
    fixture.detectChanges();

    expect(barEl(fixture).getAttribute('aria-label')).toBe('Loading calendar events');
  });
});
