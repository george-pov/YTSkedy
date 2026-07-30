import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, RouterOutlet } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';

import { pendingChangesGuard, type PendingChangesAware } from './pending-changes-guard';

@Component({ selector: 'spec-guarded-editor', template: '' })
class StubGuardedEditor implements PendingChangesAware {
  static canDeactivate = true;

  canDeactivateWithPendingChanges(): boolean {
    return StubGuardedEditor.canDeactivate;
  }
}

@Component({ selector: 'spec-destination', template: '' })
class StubDestination {}

@Component({
  selector: 'spec-routing-shell',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
class StubShell {}

describe('pendingChangesGuard route integration', () => {
  beforeEach(() => {
    StubGuardedEditor.canDeactivate = true;

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([
          {
            path: 'editor',
            component: StubGuardedEditor,
            canDeactivate: [pendingChangesGuard],
          },
          { path: 'destination', component: StubDestination },
        ]),
      ],
    });
  });

  async function startOnGuardedEditor(): Promise<Router> {
    const shell = TestBed.createComponent(StubShell);
    shell.detectChanges();

    const router = TestBed.inject(Router);
    expect(await router.navigateByUrl('/editor')).toBe(true);
    expect(router.url).toBe('/editor');
    return router;
  }

  it('keeps the router on the guarded editor when deactivation is blocked', async () => {
    const router = await startOnGuardedEditor();
    StubGuardedEditor.canDeactivate = false;

    expect(await router.navigateByUrl('/destination')).toBe(false);
    expect(router.url).toBe('/editor');
  });

  it('permits navigation to the destination when deactivation is allowed', async () => {
    const router = await startOnGuardedEditor();

    expect(await router.navigateByUrl('/destination')).toBe(true);
    expect(router.url).toBe('/destination');
  });
});
