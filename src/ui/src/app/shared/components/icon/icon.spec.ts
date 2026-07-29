import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { Icon, supportedIconNames } from './icon';

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

  it('renders the requested Material symbol', () => {
    const matIcon = fixture.nativeElement.querySelector('mat-icon');

    expect(matIcon?.textContent?.trim()).toBe('add');
    fixture.componentRef.setInput('name', 'delete');
    fixture.detectChanges();

    expect(matIcon?.textContent?.trim()).toBe('delete');
    expect(matIcon?.classList).toContain('material-symbols-outlined');
    expect(fixture.nativeElement.querySelector('svg')).toBeNull();
  });

  it.each(supportedIconNames)('renders the supported %s icon', (name) => {
    fixture.componentRef.setInput('name', name);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('mat-icon')?.textContent?.trim()).toBe(name);
  });

  it('uses the standard Material icon size and inherited current color', () => {
    const host = fixture.nativeElement as HTMLElement;
    host.style.color = 'rgb(12, 34, 56)';

    expect(getComputedStyle(host).width).toBe('24px');
    expect(getComputedStyle(host).height).toBe('24px');
    expect(getComputedStyle(host).color).toBe('rgb(12, 34, 56)');
  });

  it('is decorative and cannot receive focus', () => {
    const host = fixture.nativeElement as HTMLElement;
    const matIcon = host.querySelector('mat-icon');

    expect(host.getAttribute('aria-hidden')).toBe('true');
    expect(matIcon?.getAttribute('aria-hidden')).toBe('true');
    expect(matIcon?.getAttribute('tabindex')).toBeNull();
  });
});
