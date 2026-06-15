import { inject, Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/**
 * App-owned wrapper over Angular Material's snackbar for transient,
 * page-agnostic notifications such as a success confirmation. Keeping the
 * snackbar behind this service means pages depend on app intent
 * (`showSuccess`) rather than the Material API, so the toast implementation
 * stays swappable.
 *
 * Use this for positive, non-blocking feedback that may be missed without harm.
 * Errors that need to persist or be acted on belong in an inline
 * `app-alert`, not a snackbar.
 */
@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  /**
   * Shows a transient success message. It auto-dismisses after a few seconds
   * and is announced politely, with a manual dismiss action for users who want
   * to close it early.
   */
  showSuccess(message: string): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: 5000,
      politeness: 'polite',
      panelClass: 'app-snackbar--success',
    });
  }
}
