import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { Icon, type IconName } from './icon';

const supportedIcons: readonly IconName[] = [
  'add',
  'check_circle',
  'close',
  'delete',
  'edit',
  'error',
  'info',
  'logout',
  'menu',
  'publish',
  'save',
  'upload',
  'visibility',
  'warning',
];

describe('Icon', () => {
  let fixture: ComponentFixture<Icon>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(Icon);
    fixture.componentRef.setInput('name', 'add');
    fixture.detectChanges();
  });

  it('renders the requested local SVG path', () => {
    const path = fixture.nativeElement.querySelector('path');

    expect(path?.getAttribute('d')).toBe('M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z');

    fixture.componentRef.setInput('name', 'delete');
    fixture.detectChanges();

    expect(path?.getAttribute('d')).toBe(
      'M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z',
    );
  });

  it.each(supportedIcons)('renders the supported %s icon', (name) => {
    fixture.componentRef.setInput('name', name);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('path')?.getAttribute('d')).toBeTruthy();
  });

  it('uses the standard Material icon size and inherited current color', () => {
    const host = fixture.nativeElement as HTMLElement;
    const svg = host.querySelector('svg') as SVGElement;
    host.style.color = 'rgb(12, 34, 56)';

    expect(getComputedStyle(host).width).toBe('24px');
    expect(getComputedStyle(host).height).toBe('24px');
    expect(getComputedStyle(host).color).toBe('rgb(12, 34, 56)');
    expect(getComputedStyle(svg).fill).toBe('currentcolor');
  });

  it('is decorative and cannot receive focus', () => {
    const host = fixture.nativeElement as HTMLElement;
    const svg = host.querySelector('svg');

    expect(host.classList).toContain('mat-icon');
    expect(host.classList).toContain('mat-icon-no-color');
    expect(host.getAttribute('aria-hidden')).toBe('true');
    expect(svg?.getAttribute('focusable')).toBe('false');
  });
});
