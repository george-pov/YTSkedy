import { describe, expect, it } from 'vitest';

import { FakeAuthFacade } from './fake-auth-facade';
import { UserIdentity } from './user-identity';

describe('FakeAuthFacade', () => {
  it('starts unauthenticated by default', () => {
    const fake = new FakeAuthFacade();

    expect(fake.isAuthenticated()).toBe(false);
  });

  it('records signIn calls and becomes authenticated', async () => {
    const fake = new FakeAuthFacade();

    await fake.signIn('/calendar-events');
    await fake.signIn();

    expect(fake.isAuthenticated()).toBe(true);
    expect(fake.signInCalls).toEqual(['/calendar-events', '']);
  });

  it('records signOut and becomes unauthenticated', async () => {
    const fake = new FakeAuthFacade({ authenticated: true });

    await fake.signOut();

    expect(fake.isAuthenticated()).toBe(false);
    expect(fake.signOutCalls).toBe(1);
  });

  it('returns the configured token from acquireApiToken', async () => {
    const fake = new FakeAuthFacade({ apiToken: 'token-abc' });

    const token = await fake.acquireApiToken(['scope.a', 'scope.b']);

    expect(token).toBe('token-abc');
    expect(fake.acquireApiTokenCalls).toEqual([['scope.a', 'scope.b']]);
  });

  it('throws the configured error when acquireApiToken is rigged to fail', async () => {
    const fake = new FakeAuthFacade({
      apiTokenError: new Error('rigged failure'),
    });

    await expect(fake.acquireApiToken(['scope.a'])).rejects.toThrow();
  });

  it('returns a default identity', () => {
    const fake = new FakeAuthFacade();

    expect(fake.getUserIdentity()).toEqual({ name: 'Jane Doe' });
  });

  it('returns the configured identity', () => {
    const identity: UserIdentity = { givenName: 'Ada', familyName: 'Lovelace' };
    const fake = new FakeAuthFacade({ identity });

    expect(fake.getUserIdentity()).toEqual(identity);
  });

  it('returns null when identity is explicitly null', () => {
    const fake = new FakeAuthFacade({ identity: null });

    expect(fake.getUserIdentity()).toBeNull();
  });
});
