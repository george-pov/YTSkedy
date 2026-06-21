import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  AuthenticationResult,
  InteractionRequiredAuthError,
} from '@azure/msal-browser';
import { MsalService } from '@azure/msal-angular';

import { clearRecoveryFlag } from './auth-recovery';
import { trimToUndefined } from './trim-to-undefined';
import { UserIdentity } from './user-identity';

export abstract class AuthFacade {
  abstract isAuthenticated(): boolean;
  abstract signIn(returnUrl?: string): Promise<void>;
  abstract signOut(): Promise<void>;
  abstract acquireApiToken(scopes: string[]): Promise<string>;
  abstract getUserIdentity(): UserIdentity | null;
}

@Injectable()
export class MsalAuthFacade extends AuthFacade {
  private readonly msal = inject(MsalService);

  isAuthenticated(): boolean {
    return this.msal.instance.getActiveAccount() !== null;
  }

  getUserIdentity(): UserIdentity | null {
    const account = this.msal.instance.getActiveAccount();
    if (account === null) {
      return null;
    }

    const claims = (account.idTokenClaims ?? {}) as Record<string, unknown>;
    return {
      name: claimString(claims, 'name') ?? trimToUndefined(account.name),
      givenName: claimString(claims, 'given_name'),
      familyName: claimString(claims, 'family_name'),
      email:
        claimString(claims, 'email') ??
        claimString(claims, 'preferred_username'),
    };
  }

  async signIn(returnUrl?: string): Promise<void> {
    await firstValueFrom(
      this.msal.loginRedirect({
        scopes: [],
        state: returnUrl,
      }),
    );
  }

  async signOut(): Promise<void> {
    // Drop the interceptor's one-shot recovery flag so a same-tab
    // sign-out / sign-in cycle does not suppress the next legitimate
    // 401-driven recovery.
    clearRecoveryFlag();
    await firstValueFrom(
      this.msal.logoutRedirect({
        account: this.msal.instance.getActiveAccount() ?? undefined,
      }),
    );
  }

  async acquireApiToken(scopes: string[]): Promise<string> {
    const account = this.msal.instance.getActiveAccount();
    if (account === null) {
      throw new Error('Cannot acquire API token without an active account.');
    }

    try {
      const result = await firstValueFrom(
        this.msal.acquireTokenSilent({ scopes, account }),
      );
      return this.extractAccessToken(result);
    } catch (error) {
      if (error instanceof InteractionRequiredAuthError) {
        // Fire the redirect, then suspend this promise indefinitely. The
        // browser is about to navigate away to Entra, so re-throwing here
        // would race the interceptor's catchError into firing a second
        // sign-in redirect in the same tick.
        await firstValueFrom(this.msal.acquireTokenRedirect({ scopes }));
        return new Promise<string>(() => {});
      }
      throw error;
    }
  }

  private extractAccessToken(result: AuthenticationResult): string {
    if (result.accessToken.length === 0) {
      throw new Error('MSAL returned an empty access token.');
    }
    return result.accessToken;
  }
}

function claimString(
  claims: Record<string, unknown>,
  key: string,
): string | undefined {
  const value = claims[key];
  return typeof value === 'string' ? trimToUndefined(value) : undefined;
}
