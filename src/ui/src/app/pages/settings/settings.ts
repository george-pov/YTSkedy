import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { form } from '@angular/forms/signals';
import { finalize } from 'rxjs';

import { EventTextFieldsService } from 'src/app/shared/api/settings/event-text-fields-service';
import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { Input } from 'src/app/shared/components/input/input';
import { delayedLoading } from 'src/app/shared/components/progress-bar/delayed-loading';
import { ProgressBar } from 'src/app/shared/components/progress-bar/progress-bar';
import { Select, SelectOption } from 'src/app/shared/components/select/select';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import {
  appendEventTextField,
  applySettingsRules,
  createSettingsModel,
  deleteEventTextField,
  toUpdateEventTextFieldsRequest,
  type SettingsModel,
} from './settings.form';

@Component({
  selector: 'app-settings',
  imports: [Alert, Button, Input, ProgressBar, Select],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Settings implements OnInit {
  private readonly eventTextFields = inject(EventTextFieldsService);
  private readonly notifications = inject(NotificationService);

  protected readonly model = signal<SettingsModel>(createSettingsModel());
  protected readonly form = form(this.model, applySettingsRules);

  protected readonly typeOptions: readonly SelectOption[] = [
    { value: 'ShortText', label: 'Short text' },
    { value: 'LongText', label: 'Long text' },
  ];

  protected readonly isLoading = signal(true);
  protected readonly showLoading = delayedLoading(() => this.isLoading());
  protected readonly loadFailed = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly saveErrorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadFields();
  }

  protected addField(): void {
    this.model.update(({ fields }) => ({
      fields: appendEventTextField(fields),
    }));
  }

  protected deleteField(index: number): void {
    this.model.update(({ fields }) => ({ fields: deleteEventTextField(fields, index) }));
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    this.save();
  }

  protected save(): void {
    if (this.isSaving() || this.isLoading()) {
      return;
    }

    this.saveErrorMessage.set(null);

    if (this.form().invalid()) {
      this.form().markAsTouched();
      return;
    }

    this.isSaving.set(true);

    this.eventTextFields
      .update(toUpdateEventTextFieldsRequest(this.model()))
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (response) => {
          this.model.set(createSettingsModel(response.fields));
          this.notifications.showSuccess('Event text fields saved.');
        },
        error: () => {
          this.saveErrorMessage.set('Event text fields could not be saved. Try again.');
        },
      });
  }

  private loadFields(): void {
    this.isLoading.set(true);
    this.loadFailed.set(false);

    this.eventTextFields
      .get()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (response) => {
          this.model.set(createSettingsModel(response.fields));
        },
        error: () => {
          this.model.set(createSettingsModel());
          this.loadFailed.set(true);
        },
      });
  }
}
