import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { StatusPill, type StatusPillVariant } from './status-pill';

@Component({
  selector: 'app-status-pill-host',
  imports: [StatusPill],
  template: `
    <app-status-pill [variant]="variant()">Published</app-status-pill>
  `,
})
class StatusPillHost {
  readonly variant = signal<StatusPillVariant>('neutral');
}

function pillEl(fixture: ComponentFixture<StatusPillHost>): HTMLElement {
  return fixture.nativeElement.querySelector('app-status-pill') as HTMLElement;
}

describe('StatusPill', () => {
  let fixture: ComponentFixture<StatusPillHost>;
  let host: StatusPillHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(StatusPillHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('projects the pill text', () => {
    expect(pillEl(fixture).textContent).toContain('Published');
  });

  it('applies the variant class', () => {
    host.variant.set('warning');
    fixture.detectChanges();

    expect(pillEl(fixture).classList).toContain('warning');
  });
});
