import { AuthFacade } from './auth-facade';
import { UserIdentity } from './user-identity';

export interface FakeAuthFacadeState {
  authenticated?: boolean;
  apiToken?: string;
  apiTokenError?: Error;
  identity?: UserIdentity | null;
}

export class FakeAuthFacade extends AuthFacade {
  authenticated: boolean;
  apiToken: string;
  apiTokenError?: Error;
  identity: UserIdentity | null;

  signInCalls: string[] = [];
  signOutCalls = 0;
  acquireApiTokenCalls: string[][] = [];

  constructor(state: FakeAuthFacadeState = {}) {
    super();
    this.authenticated = state.authenticated ?? false;
    this.apiToken = state.apiToken ?? 'fake-access-token';
    this.apiTokenError = state.apiTokenError;
    this.identity =
      state.identity === undefined ? { name: 'Jane Doe' } : state.identity;
  }

  isAuthenticated(): boolean {
    return this.authenticated;
  }

  getUserIdentity(): UserIdentity | null {
    return this.identity;
  }

  async signIn(returnUrl?: string): Promise<void> {
    this.signInCalls.push(returnUrl ?? '');
    this.authenticated = true;
  }

  async signOut(): Promise<void> {
    this.signOutCalls += 1;
    this.authenticated = false;
  }

  async acquireApiToken(scopes: string[]): Promise<string> {
    this.acquireApiTokenCalls.push([...scopes]);
    if (this.apiTokenError !== undefined) {
      throw this.apiTokenError;
    }
    return this.apiToken;
  }
}
