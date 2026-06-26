import { Component, provideZonelessChangeDetection, signal, Signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { delayedLoading } from './delayed-loading';

const APPEAR_DELAY_MS = 200;
const MIN_VISIBLE_MS = 400;

@Component({
  selector: 'app-delayed-loading-host',
  template: '',
})
class DelayedLoadingHost {
  readonly source = signal(false);
  readonly visible: Signal<boolean> = delayedLoading(this.source, {
    appearDelayMs: APPEAR_DELAY_MS,
    minVisibleMs: MIN_VISIBLE_MS,
  });
}

describe('delayedLoading', () => {
  let fixture: ComponentFixture<DelayedLoadingHost>;
  let host: DelayedLoadingHost;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(DelayedLoadingHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  // Run the effect so it observes the latest source value and (re)schedules.
  function flush(): void {
    fixture.detectChanges();
  }

  it('stays hidden while the source flips on and off within the grace period', () => {
    host.source.set(true);
    flush();
    vi.advanceTimersByTime(APPEAR_DELAY_MS - 1);
    host.source.set(false);
    flush();

    vi.advanceTimersByTime(10_000);
    expect(host.visible()).toBe(false);
  });

  it('shows the indicator only after the source stays true for the grace period', () => {
    host.source.set(true);
    flush();

    vi.advanceTimersByTime(APPEAR_DELAY_MS - 1);
    expect(host.visible()).toBe(false);

    vi.advanceTimersByTime(1);
    expect(host.visible()).toBe(true);
  });

  it('keeps the indicator visible for the minimum time after the source clears', () => {
    host.source.set(true);
    flush();
    vi.advanceTimersByTime(APPEAR_DELAY_MS);
    expect(host.visible()).toBe(true);

    host.source.set(false);
    flush();

    vi.advanceTimersByTime(MIN_VISIBLE_MS - 1);
    expect(host.visible()).toBe(true);

    vi.advanceTimersByTime(1);
    expect(host.visible()).toBe(false);
  });

  it('hides immediately when the source clears after the minimum visible time', () => {
    host.source.set(true);
    flush();
    vi.advanceTimersByTime(APPEAR_DELAY_MS + MIN_VISIBLE_MS);
    expect(host.visible()).toBe(true);

    host.source.set(false);
    flush();
    expect(host.visible()).toBe(false);
  });

  it('cancels a pending hide when the source becomes true again', () => {
    host.source.set(true);
    flush();
    vi.advanceTimersByTime(APPEAR_DELAY_MS);

    host.source.set(false);
    flush();
    vi.advanceTimersByTime(MIN_VISIBLE_MS - 100);

    host.source.set(true);
    flush();
    vi.advanceTimersByTime(10_000);
    expect(host.visible()).toBe(true);
  });

  it('clears pending timers on destroy', () => {
    host.source.set(true);
    flush();
    fixture.destroy();

    vi.advanceTimersByTime(10_000);
    expect(host.visible()).toBe(false);
  });
});
