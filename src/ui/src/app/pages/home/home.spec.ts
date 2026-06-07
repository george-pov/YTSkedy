import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { describe, expect, it } from 'vitest';

import { AuthFacade } from 'src/app/shared/auth/auth-facade';
import { FakeAuthFacade } from 'src/app/shared/auth/fake-auth-facade';
import { Home } from './home';

function configure(fake: FakeAuthFacade) {
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      provideRouter([]),
      { provide: AuthFacade, useValue: fake },
    ],
  });
}

describe('Home', () => {
  it('renders the title and invite-only orientation copy', () => {
    const fake = new FakeAuthFacade({ authenticated: false });
    configure(fake);

    const fixture = TestBed.createComponent(Home);
    fixture.detectChanges();

    const title = fixture.nativeElement.querySelector('.title');
    const intro = fixture.nativeElement.querySelector('.intro');

    expect(title?.textContent?.trim()).toBe('YTSkedy');
    expect(intro?.textContent?.toLowerCase()).toContain('invite-only');
  });

  it('starts sign-in with the calendar-events return URL when the button is clicked', async () => {
    const fake = new FakeAuthFacade({ authenticated: false });
    configure(fake);

    const fixture = TestBed.createComponent(Home);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('app-button');
    expect(button).not.toBeNull();
    button.dispatchEvent(new Event('click'));

    await fixture.whenStable();

    expect(fake.signInCalls).toEqual(['/calendar-events']);
  });
});
