import { firstValueFrom, Observable, of } from 'rxjs';
import { describe, expect, it } from 'vitest';

import {
  pendingChangesGuard,
  type PendingChangesAware,
} from './pending-changes-guard';

describe('pendingChangesGuard', () => {
  it('returns synchronous true decisions', () => {
    const result = runGuard({
      canDeactivateWithPendingChanges: () => true,
    });

    expect(result).toBe(true);
  });

  it('returns synchronous false decisions', () => {
    const result = runGuard({
      canDeactivateWithPendingChanges: () => false,
    });

    expect(result).toBe(false);
  });

  it('returns promise decisions', async () => {
    const result = runGuard({
      canDeactivateWithPendingChanges: () => Promise.resolve(true),
    }) as Promise<boolean>;

    await expect(result).resolves.toBe(true);
  });

  it('returns observable decisions', async () => {
    const result = runGuard({
      canDeactivateWithPendingChanges: () => of(false),
    }) as Observable<boolean>;

    await expect(firstValueFrom(result)).resolves.toBe(false);
  });

  function runGuard(component: PendingChangesAware) {
    return pendingChangesGuard(
      component,
      null!,
      null!,
      null!,
    );
  }
});
