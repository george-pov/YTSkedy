import { AuthFacade } from './auth-facade';

export interface FakeAuthFacadeState {
  authenticated?: boolean;
  apiToken?: string;
  apiTokenError?: Error;
}

export class FakeAuthFacade extends AuthFacade {
  authenticated: boolean;
  apiToken: string;
  apiTokenError?: Error;

  signInCalls: string[] = [];
  signOutCalls = 0;
  acquireApiTokenCalls: string[][] = [];

  constructor(state: FakeAuthFacadeState = {}) {
    super();
    this.authenticated = state.authenticated ?? false;
    this.apiToken = state.apiToken ?? 'fake-access-token';
    this.apiTokenError = state.apiTokenError;
  }

  isAuthenticated(): boolean {
    return this.authenticated;
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
