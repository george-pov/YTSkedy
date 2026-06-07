import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { beforeEach, describe, expect, it } from 'vitest';

import { AuthFacade } from 'src/app/shared/auth/auth-facade';
import { FakeAuthFacade } from 'src/app/shared/auth/fake-auth-facade';
import { AppLayout } from './app-layout';

function configure(fake: FakeAuthFacade) {
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      provideRouter([]),
      { provide: AuthFacade, useValue: fake },
    ],
  });
}

describe('AppLayout', () => {
  let fake: FakeAuthFacade;

  beforeEach(() => {
    fake = new FakeAuthFacade();
  });

  it('hides the sign-out control when unauthenticated', () => {
    fake = new FakeAuthFacade({ authenticated: false });
    configure(fake);

    const fixture = TestBed.createComponent(AppLayout);
    fixture.detectChanges();

    const signOutButton = fixture.nativeElement.querySelector('.header-row app-button');
    expect(signOutButton).toBeNull();
  });

  it('shows the sign-out control when authenticated', () => {
    fake = new FakeAuthFacade({ authenticated: true });
    configure(fake);

    const fixture = TestBed.createComponent(AppLayout);
    fixture.detectChanges();

    const signOutButton = fixture.nativeElement.querySelector('.header-row app-button');
    expect(signOutButton).not.toBeNull();
  });

  it('signs out when the sign-out control is clicked', async () => {
    fake = new FakeAuthFacade({ authenticated: true });
    configure(fake);

    const fixture = TestBed.createComponent(AppLayout);
    fixture.detectChanges();

    const signOutButton = fixture.nativeElement.querySelector('.header-row app-button');
    signOutButton.dispatchEvent(new Event('click'));

    await fixture.whenStable();

    expect(fake.signOutCalls).toBe(1);
  });
});
