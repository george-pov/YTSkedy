import { DestroyRef, effect, inject, signal, Signal, untracked } from '@angular/core';

/** Tuning for {@link delayedLoading}. Durations are in milliseconds. */
export interface DelayedLoadingOptions {
  /**
   * Grace period to wait before reporting `true`. If the source flag clears
   * within this window the result never flips, so quick operations show no
   * indicator. Defaults to 200.
   */
  readonly appearDelayMs?: number;
  /**
   * Once the result is `true`, keep it `true` for at least this long even if
   * the source clears sooner, so the indicator cannot flash in and out.
   * Defaults to 400.
   */
  readonly minVisibleMs?: number;
}

const DEFAULT_APPEAR_DELAY_MS = 200;
const DEFAULT_MIN_VISIBLE_MS = 400;

/**
 * Wraps a boolean loading flag so a transient `true` does not flicker a loading
 * indicator. The returned signal stays `false` until `source` has been `true`
 * for `appearDelayMs`, then stays `true` for at least `minVisibleMs`. Pair it
 * with an indicator gate, for example `@if (showLoading())`.
 *
 * Must be called in an injection context, such as a component field
 * initializer. It owns an `effect` and clears its timers on destroy.
 */
export function delayedLoading(
  source: () => boolean,
  options: DelayedLoadingOptions = {},
): Signal<boolean> {
  const appearDelayMs = options.appearDelayMs ?? DEFAULT_APPEAR_DELAY_MS;
  const minVisibleMs = options.minVisibleMs ?? DEFAULT_MIN_VISIBLE_MS;

  const visible = signal(false);
  let shownAt = 0;
  let appearTimer: ReturnType<typeof setTimeout> | undefined;
  let hideTimer: ReturnType<typeof setTimeout> | undefined;

  const clearAppearTimer = (): void => {
    if (appearTimer !== undefined) {
      clearTimeout(appearTimer);
      appearTimer = undefined;
    }
  };

  const clearHideTimer = (): void => {
    if (hideTimer !== undefined) {
      clearTimeout(hideTimer);
      hideTimer = undefined;
    }
  };

  const sync = (loading: boolean): void => {
    if (loading) {
      // A new or continuing operation: cancel any pending hide and schedule the
      // delayed appearance unless it is already shown or already scheduled.
      clearHideTimer();
      if (!visible() && appearTimer === undefined) {
        appearTimer = setTimeout(() => {
          appearTimer = undefined;
          shownAt = Date.now();
          visible.set(true);
        }, appearDelayMs);
      }
      return;
    }

    // The operation finished. If it finished within the grace period the
    // indicator was never shown, so just drop the pending appearance.
    clearAppearTimer();
    if (!visible() || hideTimer !== undefined) {
      return;
    }

    const remaining = minVisibleMs - (Date.now() - shownAt);
    if (remaining <= 0) {
      visible.set(false);
      return;
    }

    hideTimer = setTimeout(() => {
      hideTimer = undefined;
      visible.set(false);
    }, remaining);
  };

  effect(() => {
    const loading = source();
    untracked(() => sync(loading));
  });

  inject(DestroyRef).onDestroy(() => {
    clearAppearTimer();
    clearHideTimer();
  });

  return visible.asReadonly();
}
