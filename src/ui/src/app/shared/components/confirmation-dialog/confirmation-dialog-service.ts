import { inject, Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable } from 'rxjs';

import {
  ConfirmationDialog,
  ConfirmationDialogContent,
  ConfirmationDialogData,
} from './confirmation-dialog';

// Backs the unique aria-describedby id given to each opened dialog's body text,
// so a screen reader announcing an alertdialog reads the message, not just the
// title.
let uniqueBodyId = 0;

/**
 * App-owned wrapper over Angular Material's dialog for confirmations. Pages
 * depend on app intent (`confirm`) rather than the Material API, so the dialog
 * implementation stays swappable and every confirmation shares one accessible,
 * consistently styled surface.
 *
 * Use it for decisions that should interrupt the user and require an explicit
 * choice, such as deleting a calendar event, publishing to YouTube, or leaving
 * a form with unsaved changes.
 */
@Injectable({
  providedIn: 'root',
})
export class ConfirmationDialogService {
  private readonly dialog = inject(MatDialog);

  /**
   * Opens a modal confirmation and resolves with the chosen action id, or
   * `undefined` when the user dismisses the dialog with Escape or a backdrop
   * click. The emitted id is typed from the supplied actions.
   */
  confirm<TActionId extends string = string>(
    data: ConfirmationDialogData<TActionId>,
  ): Observable<TActionId | undefined> {
    const bodyId = `confirmation-dialog-body-${(uniqueBodyId += 1)}`;
    const content: ConfirmationDialogContent = { ...data, bodyId };

    return this.dialog
      .open<ConfirmationDialog, ConfirmationDialogContent, TActionId>(
        ConfirmationDialog,
        {
          data: content,
          // alertdialog + aria-describedby is the accessible pairing for a
          // prompt that interrupts the user and needs a response.
          role: 'alertdialog',
          ariaDescribedBy: bodyId,
          width: '28rem',
          maxWidth: '92vw',
          // Focus the first action (order the safe choice first) and return
          // focus to the trigger when the dialog closes.
          autoFocus: 'first-tabbable',
          restoreFocus: true,
        },
      )
      .afterClosed();
  }
}
