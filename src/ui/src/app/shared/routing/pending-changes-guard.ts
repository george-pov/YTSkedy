import {
  CanDeactivateFn,
  GuardResult,
  MaybeAsync,
} from '@angular/router';

export interface PendingChangesAware {
  canDeactivateWithPendingChanges(): MaybeAsync<GuardResult>;
}

export const pendingChangesGuard: CanDeactivateFn<PendingChangesAware> = (component) =>
  component.canDeactivateWithPendingChanges();
