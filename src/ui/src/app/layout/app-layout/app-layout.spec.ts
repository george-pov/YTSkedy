import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { Observable, of } from 'rxjs';
import { afterEach, describe, expect, it } from 'vitest';

import { AuthFacade } from 'src/app/shared/auth/auth-facade';
import { FakeAuthFacade } from 'src/app/shared/auth/fake-auth-facade';
import { AppLayout } from './app-layout';

// Force the wide layout so the user badge renders its name span deterministically
// instead of depending on the jsdom media matcher.
class FakeBreakpointObserver {
  observe(): Observable<BreakpointState> {
    return of({ matches: false, breakpoints: {} });
  }
}

function configure(fake: FakeAuthFacade) {
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      provideRouter([]),
      { provide: AuthFacade, useValue: fake },
      {
        provide: BreakpointObserver,
        useValue: new FakeBreakpointObserver() as unknown as BreakpointObserver,
      },
    ],
  });
}

describe('AppLayout', () => {
  afterEach(() => {
    document.querySelectorAll('.cdk-overlay-container').forEach((element) => element.remove());
  });

  it('hides the user badge when unauthenticated', () => {
    configure(new FakeAuthFacade({ authenticated: false }));

    const fixture = TestBed.createComponent(AppLayout);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-user-badge')).toBeNull();
  });

  it('shows the signed-in user in the badge when authenticated', () => {
    configure(
      new FakeAuthFacade({
        authenticated: true,
        identity: { name: 'Jane Doe' },
      }),
    );

    const fixture = TestBed.createComponent(AppLayout);
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('app-user-badge');
    expect(badge).not.toBeNull();
    expect(badge.querySelector('.name')?.textContent?.trim()).toBe('Jane Doe');
  });

  it('signs out when the badge Sign Out menu item is activated', async () => {
    const fake = new FakeAuthFacade({
      authenticated: true,
      identity: { name: 'Jane Doe' },
    });
    configure(fake);

    const fixture = TestBed.createComponent(AppLayout);
    fixture.detectChanges();

    const trigger = fixture.nativeElement.querySelector(
      'app-user-badge button.badge',
    ) as HTMLButtonElement;
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const item = document.querySelector('[role="menuitem"]') as HTMLButtonElement | null;
    expect(item).not.toBeNull();
    expect(item?.textContent).toContain('Sign Out');
    item?.click();

    expect(fake.signOutCalls).toBe(1);
  });
});
