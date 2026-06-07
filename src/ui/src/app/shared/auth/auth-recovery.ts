// Single-shot guard against 401-driven sign-in loops. Owned here (not in
// the interceptor) so the facade can clear it on explicit sign-out;
// otherwise a same-tab sign-out / sign-in cycle would suppress the next
// legitimate 401-driven recovery.
const RECOVERY_FLAG_KEY = 'ytskedy.auth.recoveryInProgress';

export function hasRecoveryFlag(): boolean {
  try {
    return sessionStorage.getItem(RECOVERY_FLAG_KEY) === 'true';
  } catch {
    return false;
  }
}

export function setRecoveryFlag(): void {
  try {
    sessionStorage.setItem(RECOVERY_FLAG_KEY, 'true');
  } catch {
    // Best-effort: if sessionStorage is unavailable, recovery just runs
    // on each 401 instead of being one-shot.
  }
}

export function clearRecoveryFlag(): void {
  try {
    sessionStorage.removeItem(RECOVERY_FLAG_KEY);
  } catch {
    // No-op.
  }
}
