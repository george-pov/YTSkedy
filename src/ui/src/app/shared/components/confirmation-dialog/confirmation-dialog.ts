import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';

import { Button, type LabeledButtonVariant } from 'src/app/shared/components/button/button';
import { Icon, type IconName } from 'src/app/shared/components/icon/icon';

/**
 * Visual treatment for a confirmation. Mirrors the alert variants so a delete,
 * publish, or "leave without saving" prompt reads with the same color and icon
 * language used elsewhere in the app.
 */
export type ConfirmationKind = 'success' | 'error' | 'info' | 'warning';

/**
 * One button in a confirmation. Buttons render in array order, left to right.
 * The chosen action's {@link id} is returned to the caller as the dialog
 * result.
 */
export interface ConfirmationAction<TId extends string = string> {
  /**
   * Stable identifier returned as the dialog result when this action is
   * chosen.
   */
  readonly id: TId;
  /** Visible button label. */
  readonly label: string;
  /**
   * App-owned labeled button variant. Defaults to `filled` for the
   * {@link primary} action and `text` for the rest.
   */
  readonly variant?: LabeledButtonVariant;
  /** Marks the confirming action. It renders `filled` unless overridden. */
  readonly primary?: boolean;
}

/**
 * Inputs for a confirmation. Open a dialog through
 * {@link ConfirmationDialogService.confirm} rather than constructing this
 * directly.
 */
export interface ConfirmationDialogData<TId extends string = string> {
  /** Short, specific question shown as the dialog heading. */
  readonly title: string;
  /** Supporting text shown beside the kind icon. */
  readonly body: string;
  /** Visual treatment and icon. Defaults to `info`. */
  readonly kind?: ConfirmationKind;
  /**
   * Buttons rendered in order, left to right. Order the safe choice (such as
   * Cancel) first so it receives initial keyboard focus.
   */
  readonly actions: readonly ConfirmationAction<TId>[];
}

/**
 * View data injected into {@link ConfirmationDialog}. Augments
 * {@link ConfirmationDialogData} with the generated id the service wires to the
 * dialog container's `aria-describedby`.
 */
export interface ConfirmationDialogContent extends ConfirmationDialogData {
  readonly bodyId: string;
}

/**
 * Shared modal confirmation built on Angular Material's dialog. It renders a
 * title, a kind icon beside body text, and a row of action buttons. Selecting
 * an action closes the dialog with that action's id; dismissing it (Escape or
 * backdrop) closes with `undefined`. All Material dialog wiring stays internal
 * to this component and {@link ConfirmationDialogService}.
 */
@Component({
  selector: 'app-confirmation-dialog',
  imports: [MatDialogModule, Button, Icon],
  templateUrl: './confirmation-dialog.html',
  styleUrl: './confirmation-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmationDialog {
  private readonly dialogRef = inject<MatDialogRef<ConfirmationDialog, string>>(MatDialogRef);

  protected readonly data = inject<ConfirmationDialogContent>(MAT_DIALOG_DATA);

  protected readonly kind: ConfirmationKind = this.data.kind ?? 'info';
  protected readonly icon = kindIcons[this.kind];

  protected variantFor(action: ConfirmationAction): LabeledButtonVariant {
    return action.variant ?? (action.primary ? 'filled' : 'text');
  }

  protected select(actionId: string): void {
    this.dialogRef.close(actionId);
  }
}

const kindIcons: Record<ConfirmationKind, IconName> = {
  success: 'check_circle',
  error: 'error',
  info: 'info',
  warning: 'warning',
};
