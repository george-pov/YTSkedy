import { provideRouter, Router } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { describe, expect, it } from 'vitest';

import { SignedOut } from './signed-out';

function configure() {
  TestBed.configureTestingModule({
    providers: [provideZonelessChangeDetection(), provideRouter([])],
  });
}

describe('SignedOut', () => {
  it('renders the signed-out confirmation copy', () => {
    configure();

    const fixture = TestBed.createComponent(SignedOut);
    fixture.detectChanges();

    const title = fixture.nativeElement.querySelector('.title');
    const intro = fixture.nativeElement.querySelector('.intro');
    const button = fixture.nativeElement.querySelector('app-button');

    expect(title?.textContent?.trim()).toBe('Signed out');
    expect(intro?.textContent?.toLowerCase()).toContain('signed out');
    expect(button?.textContent?.trim()).toBe('Return home');
  });

  it('navigates to the public home when "Return home" is clicked', async () => {
    configure();

    const router = TestBed.inject(Router);
    const navigations: string[] = [];
    router.navigateByUrl = ((url: string) => {
      navigations.push(url);
      return Promise.resolve(true);
    }) as Router['navigateByUrl'];

    const fixture = TestBed.createComponent(SignedOut);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('app-button');
    expect(button).not.toBeNull();
    button.dispatchEvent(new Event('click'));

    await fixture.whenStable();

    expect(navigations).toEqual(['/']);
  });
});
