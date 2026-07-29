import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { ConfirmationDialog } from './confirmation-dialog';
import { ConfirmationDialogService } from './confirmation-dialog-service';

describe('ConfirmationDialogService', () => {
  let open: ReturnType<typeof vi.fn>;

  function configure(result?: string): ConfirmationDialogService {
    open = vi.fn(() => ({ afterClosed: () => of(result) }));
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), { provide: MatDialog, useValue: { open } }],
    });
    return TestBed.inject(ConfirmationDialogService);
  }

  function lastConfig(): MatDialogConfig {
    return open.mock.calls[0][1] as MatDialogConfig;
  }

  it('opens the ConfirmationDialog component', () => {
    configure()
      .confirm({
        title: 'Delete?',
        body: 'Body',
        actions: [{ id: 'delete', label: 'Delete' }],
      })
      .subscribe();

    expect(open).toHaveBeenCalledTimes(1);
    expect(open.mock.calls[0][0]).toBe(ConfirmationDialog);
  });

  it('opens an alertdialog and passes the supplied data through', () => {
    configure()
      .confirm({
        kind: 'warning',
        title: 'Delete?',
        body: 'This cannot be undone.',
        actions: [
          { id: 'cancel', label: 'Cancel' },
          { id: 'delete', label: 'Delete', primary: true },
        ],
      })
      .subscribe();

    const config = lastConfig();
    expect(config.role).toBe('alertdialog');
    expect(config.data?.title).toBe('Delete?');
    expect(config.data?.kind).toBe('warning');
    expect(config.data?.actions).toHaveLength(2);
  });

  it('wires aria-describedby to a generated body id', () => {
    configure()
      .confirm({
        title: 'Delete?',
        body: 'Body',
        actions: [{ id: 'delete', label: 'Delete' }],
      })
      .subscribe();

    const config = lastConfig();
    expect(typeof config.data?.bodyId).toBe('string');
    expect(config.ariaDescribedBy).toBe(config.data?.bodyId);
  });

  it('emits the selected action id', () => {
    let result: string | undefined;

    configure('delete')
      .confirm({
        title: 'Delete?',
        body: 'Body',
        actions: [{ id: 'delete', label: 'Delete' }],
      })
      .subscribe((value) => (result = value));

    expect(result).toBe('delete');
  });

  it('emits undefined when the dialog is dismissed', () => {
    let result: string | undefined = 'sentinel';

    configure(undefined)
      .confirm({
        title: 'Delete?',
        body: 'Body',
        actions: [{ id: 'delete', label: 'Delete' }],
      })
      .subscribe((value) => (result = value));

    expect(result).toBeUndefined();
  });

  it('opens deletion confirmations with the safe action first and a danger primary action', () => {
    configure()
      .confirmDeletion({
        title: 'Delete template?',
        body: 'This cannot be undone.',
        deleteLabel: 'Delete template',
      })
      .subscribe();

    expect(lastConfig().data).toEqual(
      expect.objectContaining({
        kind: 'warning',
        title: 'Delete template?',
        body: 'This cannot be undone.',
        actions: [
          { id: 'cancel', label: 'Cancel' },
          {
            id: 'delete',
            label: 'Delete template',
            primary: true,
            intent: 'danger',
          },
        ],
      }),
    );
  });

  it.each([
    ['delete', true],
    ['cancel', false],
    [undefined, false],
  ] as const)('maps deletion confirmation result %s to %s', (dialogResult, expected) => {
    let result: boolean | undefined;

    configure(dialogResult)
      .confirmDeletion({
        title: 'Delete template?',
        body: 'This cannot be undone.',
        deleteLabel: 'Delete template',
      })
      .subscribe((value) => (result = value));

    expect(result).toBe(expected);
  });
});
