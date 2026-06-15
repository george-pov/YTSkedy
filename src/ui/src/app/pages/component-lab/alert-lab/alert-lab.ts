import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { NotificationService } from 'src/app/shared/notifications/notification-service';

@Component({
  selector: 'app-alert-lab',
  imports: [Alert, Button, LabExample, LabPage],
  templateUrl: './alert-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AlertLab {
  private readonly notifications = inject(NotificationService);

  protected readonly dismissed = signal(false);

  protected onDismiss(): void {
    this.dismissed.set(true);
  }

  protected reset(): void {
    this.dismissed.set(false);
  }

  protected showSuccessToast(): void {
    this.notifications.showSuccess('Calendar event published.');
  }
}
