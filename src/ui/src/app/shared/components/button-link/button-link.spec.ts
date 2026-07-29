import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';

import { type IconName } from 'src/app/shared/components/icon/icon';
import { ButtonLink, type ButtonLinkTarget } from './button-link';

@Component({
  selector: 'app-button-link-host',
  imports: [ButtonLink],
  template: `
    <app-button-link [route]="route()" [icon]="icon()">
      {{ label() }}
    </app-button-link>
  `,
})
class ButtonLinkHost {
  readonly route = signal<ButtonLinkTarget>('/calendar-events');
  readonly icon = signal<IconName | undefined>(undefined);
  readonly label = signal('Back to events');
}

function anchorEl(fixture: ComponentFixture<ButtonLinkHost>): HTMLAnchorElement {
  return fixture.nativeElement.querySelector('a') as HTMLAnchorElement;
}

describe('ButtonLink', () => {
  let fixture: ComponentFixture<ButtonLinkHost>;
  let host: ButtonLinkHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([{ path: 'calendar-events', children: [] }]),
      ],
    });
    fixture = TestBed.createComponent(ButtonLinkHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders a native anchor with the exact route and projected label', () => {
    const anchor = anchorEl(fixture);

    expect(anchor.getAttribute('href')).toBe('/calendar-events');
    expect(anchor.textContent?.trim()).toBe('Back to events');
    expect(fixture.nativeElement.querySelector('button')).toBeNull();
  });

  it('renders an optional leading icon', () => {
    host.icon.set('edit');
    fixture.detectChanges();

    const anchor = anchorEl(fixture);
    const icon = anchor.querySelector('app-icon');

    expect(icon?.querySelector('mat-icon')?.textContent?.trim()).toBe('edit');
    expect(anchor.textContent).toContain('Back to events');
  });
});
