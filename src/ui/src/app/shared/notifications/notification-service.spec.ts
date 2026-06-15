import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { NotificationService } from './notification-service';

describe('NotificationService', () => {
  let open: ReturnType<typeof vi.fn>;
  let service: NotificationService;

  beforeEach(() => {
    open = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        { provide: MatSnackBar, useValue: { open } },
      ],
    });
    service = TestBed.inject(NotificationService);
  });

  it('opens a polite, auto-dismissing snackbar with a dismiss action for success', () => {
    service.showSuccess('Calendar event published.');

    expect(open).toHaveBeenCalledTimes(1);
    const [message, action, config] = open.mock.calls[0];
    expect(message).toBe('Calendar event published.');
    expect(action).toBe('Dismiss');
    expect(config).toMatchObject({
      duration: 5000,
      politeness: 'polite',
    });
  });
});
