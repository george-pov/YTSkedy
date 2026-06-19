import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { Observable, of } from 'rxjs';
import { afterEach, describe, expect, it } from 'vitest';

import { UserBadge } from './user-badge';

@Component({
  selector: 'app-user-badge-host',
  imports: [UserBadge],
  template: `<app-user-badge
    [initials]="initials()"
    [fullName]="fullName()"
    (signOut)="signOutCount = signOutCount + 1"
  />`,
})
class UserBadgeHost {
  readonly initials = signal('JD');
  readonly fullName = signal('Jane Doe');
  signOutCount = 0;
}

// Minimal stand-in so the compact/wide layout is deterministic in tests instead
// of depending on the jsdom media matcher.
class FakeBreakpointObserver {
  constructor(private readonly matches: boolean) {}

  observe(): Observable<BreakpointState> {
    return of({ matches: this.matches, breakpoints: {} });
  }
}

function setup(compact = false): {
  fixture: ComponentFixture<UserBadgeHost>;
  host: UserBadgeHost;
} {
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      {
        provide: BreakpointObserver,
        useValue: new FakeBreakpointObserver(
          compact,
        ) as unknown as BreakpointObserver,
      },
    ],
  });
  const fixture = TestBed.createComponent(UserBadgeHost);
  const host = fixture.componentInstance;
  fixture.detectChanges();
  return { fixture, host };
}

function badgeEl(fixture: ComponentFixture<UserBadgeHost>): HTMLElement {
  return fixture.nativeElement.querySelector('button.badge') as HTMLElement;
}

describe('UserBadge', () => {
  afterEach(() => {
    document
      .querySelectorAll('.cdk-overlay-container')
      .forEach((element) => element.remove());
  });

  it('renders the provided initials in the monogram', () => {
    const { fixture } = setup();
    expect(
      fixture.nativeElement.querySelector('.monogram')?.textContent?.trim(),
    ).toBe('JD');
  });

  it('shows the full name beside the monogram on wide screens', () => {
    const { fixture } = setup();
    expect(
      fixture.nativeElement.querySelector('.name')?.textContent?.trim(),
    ).toBe('Jane Doe');
  });

  it('hides the name and keeps only the monogram when compact', () => {
    const { fixture } = setup(true);
    expect(fixture.nativeElement.querySelector('.name')).toBeNull();
    expect(
      fixture.nativeElement.querySelector('.monogram')?.textContent?.trim(),
    ).toBe('JD');
  });

  it('labels the trigger with the full name', () => {
    const { fixture } = setup();
    expect(badgeEl(fixture).getAttribute('aria-label')).toBe(
      'Account menu for Jane Doe',
    );
  });

  it('emits signOut when the Sign Out menu item is activated', async () => {
    const { fixture, host } = setup();
    badgeEl(fixture).click();
    fixture.detectChanges();
    await fixture.whenStable();

    const item = document.querySelector(
      '.mat-mdc-menu-item',
    ) as HTMLButtonElement | null;
    expect(item).not.toBeNull();
    item?.click();

    expect(host.signOutCount).toBe(1);
  });
});
