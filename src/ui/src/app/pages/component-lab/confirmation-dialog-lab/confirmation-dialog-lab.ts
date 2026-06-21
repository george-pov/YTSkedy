import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { Button } from 'src/app/shared/components/button/button';
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';

@Component({
  selector: 'app-confirmation-dialog-lab',
  imports: [Button, LabExample, LabPage],
  templateUrl: './confirmation-dialog-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmationDialogLab {
  private readonly confirmation = inject(ConfirmationDialogService);

  protected readonly lastResult = signal('No dialog opened yet.');

  protected confirmDelete(): void {
    this.confirmation
      .confirm({
        kind: 'warning',
        title: 'Delete calendar event?',
        body: 'This permanently removes the scheduled event. This cannot be undone.',
        actions: [
          { id: 'cancel', label: 'Cancel' },
          { id: 'delete', label: 'Delete', primary: true },
        ],
      })
      .subscribe((result) => this.report(result));
  }

  protected confirmPublish(): void {
    this.confirmation
      .confirm({
        kind: 'info',
        title: 'Publish to YouTube?',
        body: 'Publishing creates a scheduled YouTube broadcast from this calendar event.',
        actions: [
          { id: 'cancel', label: 'Cancel' },
          { id: 'publish', label: 'Publish', primary: true },
        ],
      })
      .subscribe((result) => this.report(result));
  }

  protected confirmDiscard(): void {
    this.confirmation
      .confirm({
        kind: 'warning',
        title: 'Discard unsaved changes?',
        body: 'Your edits to this event have not been saved. Leaving now discards them.',
        actions: [
          { id: 'stay', label: 'Keep editing' },
          { id: 'discard', label: 'Discard changes', primary: true },
        ],
      })
      .subscribe((result) => this.report(result));
  }

  private report(result: string | undefined): void {
    this.lastResult.set(
      result === undefined ? 'Dismissed' : `Selected: ${result}`,
    );
  }
}
