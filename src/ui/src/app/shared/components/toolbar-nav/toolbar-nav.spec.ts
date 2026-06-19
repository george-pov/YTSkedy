import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';

import { ToolbarNav, ToolbarNavItem } from './toolbar-nav';

@Component({
  selector: 'app-toolbar-nav-host',
  imports: [ToolbarNav],
  template: `<app-toolbar-nav [label]="label()" [items]="items()" [align]="align()" />`,
})
class ToolbarNavHost {
  readonly label = signal('Primary navigation');
  readonly items = signal<readonly ToolbarNavItem[]>([]);
  readonly align = signal<'start' | 'end'>('start');
}

function navEl(fixture: ComponentFixture<ToolbarNavHost>): HTMLElement {
  return fixture.nativeElement.querySelector('nav') as HTMLElement;
}

describe('ToolbarNav', () => {
  let fixture: ComponentFixture<ToolbarNavHost>;
  let host: ToolbarNavHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([
          { path: 'calendar-events', children: [] },
          { path: 'settings', children: [] },
        ]),
      ],
    });
    fixture = TestBed.createComponent(ToolbarNavHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders the navigation landmark with the accessible label', () => {
    expect(navEl(fixture).getAttribute('aria-label')).toBe('Primary navigation');
  });

  it('wraps the menu in a toolbar', () => {
    expect(fixture.nativeElement.querySelector('mat-toolbar')).not.toBeNull();
  });

  it('renders a link item as an anchor pointing at its route', () => {
    host.items.set([{ label: 'Calendar Events', link: '/calendar-events' }]);
    fixture.detectChanges();

    const anchor = navEl(fixture).querySelector('a');
    expect(anchor?.textContent?.trim()).toBe('Calendar Events');
    expect(anchor?.getAttribute('href')).toBe('/calendar-events');
  });

  it('renders an action item as a button and runs its callback on click', () => {
    let activated = 0;
    host.items.set([{ label: 'Refresh', action: () => (activated += 1) }]);
    fixture.detectChanges();

    const button = navEl(fixture).querySelector('button') as HTMLButtonElement;
    expect(button.textContent?.trim()).toBe('Refresh');

    button.click();
    expect(activated).toBe(1);
  });

  it('packs items together aligned to the start by default', () => {
    expect(navEl(fixture).classList).toContain('app-actions');
    expect(navEl(fixture).classList).not.toContain('app-actions-end');
  });

  it('aligns items to the end when requested', () => {
    host.align.set('end');
    fixture.detectChanges();

    expect(navEl(fixture).classList).toContain('app-actions-end');
  });

  it('marks the link for the active route as selected', async () => {
    host.items.set([
      { label: 'Calendar Events', link: '/calendar-events' },
      { label: 'Settings', link: '/settings' },
    ]);
    fixture.detectChanges();

    await TestBed.inject(Router).navigate(['/calendar-events']);
    fixture.detectChanges();

    const active = navEl(fixture).querySelector('a.is-active');
    expect(active?.textContent?.trim()).toBe('Calendar Events');
    expect(active?.getAttribute('aria-current')).toBe('page');
  });
});
