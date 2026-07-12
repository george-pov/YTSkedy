import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  signal,
  type OnInit,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form } from '@angular/forms/signals';
import { finalize, map, Observable } from 'rxjs';

import {
  EventTextField,
  EventTextFieldsService,
  UpdateEventTextFieldsRequest,
} from 'src/app/shared/api/settings/event-text-fields-service';
import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { Input } from 'src/app/shared/components/input/input';
import { delayedLoading } from 'src/app/shared/components/progress-bar/delayed-loading';
import { ProgressBar } from 'src/app/shared/components/progress-bar/progress-bar';
import { Select, SelectOption } from 'src/app/shared/components/select/select';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { type PendingChangesAware } from 'src/app/shared/routing/pending-changes-guard';
import {
  appendEventTextField,
  applySettingsRules,
  createSettingsModel,
  deleteEventTextField,
  sameUpdateEventTextFieldsRequest,
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
export class Settings implements OnInit, PendingChangesAware {
  private readonly eventTextFields = inject(EventTextFieldsService);
  private readonly confirmation = inject(ConfirmationDialogService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly model = signal<SettingsModel>(createSettingsModel());
  protected readonly form = form(this.model, applySettingsRules);
  protected readonly savedSettingsRequest = signal<UpdateEventTextFieldsRequest | null>(null);
  protected readonly hasPendingSettingsChanges = computed(() => {
    const saved = this.savedSettingsRequest();
    return (
      saved !== null &&
      !sameUpdateEventTextFieldsRequest(toUpdateEventTextFieldsRequest(this.model()), saved)
    );
  });
  protected readonly saveDisabled = computed(
    () => this.isLoading() || this.isSaving() || !this.hasPendingSettingsChanges(),
  );

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

  canDeactivateWithPendingChanges(): boolean | Observable<boolean> {
    if (!this.hasPendingSettingsChanges() || this.isLoading() || this.isSaving()) {
      return true;
    }

    return this.confirmDiscardSettingsChanges();
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

  protected cancel(): void {
    if (this.isLoading() || this.isSaving()) {
      return;
    }

    if (!this.hasPendingSettingsChanges()) {
      this.restoreSavedSettings();
      return;
    }

    this.confirmDiscardSettingsChanges().subscribe((discard) => {
      if (discard) {
        this.restoreSavedSettings();
      }
    });
  }

  protected save(): void {
    if (this.saveDisabled()) {
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
      .pipe(
        finalize(() => this.isSaving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.applyFields(response.fields);
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
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.applyFields(response.fields);
        },
        error: () => {
          this.model.set(createSettingsModel());
          this.savedSettingsRequest.set(null);
          this.loadFailed.set(true);
        },
      });
  }

  private confirmDiscardSettingsChanges(): Observable<boolean> {
    return this.confirmation
      .confirm<'keep-editing' | 'discard'>({
        kind: 'warning',
        title: 'Discard unsaved settings changes?',
        body: 'Event text field changes have not been saved.',
        actions: [
          { id: 'keep-editing', label: 'Keep editing' },
          { id: 'discard', label: 'Discard changes', primary: true },
        ],
      })
      .pipe(
        map((result) => result === 'discard'),
        takeUntilDestroyed(this.destroyRef),
      );
  }

  private applyFields(fields: readonly EventTextField[]): void {
    this.model.set(createSettingsModel(fields));
    this.savedSettingsRequest.set(toUpdateEventTextFieldsRequest(this.model()));
    this.saveErrorMessage.set(null);
  }

  private restoreSavedSettings(): void {
    const saved = this.savedSettingsRequest();
    if (saved === null) {
      this.saveErrorMessage.set(null);
      return;
    }

    this.model.set(createSettingsModel(saved.fields));
    this.saveErrorMessage.set(null);
  }
}
