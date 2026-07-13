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
  CalendarEventDefaultsResponse,
  CalendarEventDefaultsService,
  UpdateCalendarEventDefaultsRequest,
} from 'src/app/shared/api/settings/calendar-event-defaults-service';
import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { Input } from 'src/app/shared/components/input/input';
import { delayedLoading } from 'src/app/shared/components/progress-bar/delayed-loading';
import { ProgressBar } from 'src/app/shared/components/progress-bar/progress-bar';
import { Select, SelectOption } from 'src/app/shared/components/select/select';
import { TimeField } from 'src/app/shared/components/time/time';
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
import {
  applyStartDefaultsRules,
  createStartDefaultsModel,
  sameUpdateStartDefaultsRequest,
  startDefaultsTimeZoneOptions,
  toUpdateStartDefaultsRequest,
  weekdayOptions,
  type StartDefaultsModel,
} from './start-defaults.form';

@Component({
  selector: 'app-settings',
  imports: [Alert, Button, Input, ProgressBar, Select, TimeField],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Settings implements OnInit, PendingChangesAware {
  private readonly defaults = inject(CalendarEventDefaultsService);
  private readonly confirmation = inject(ConfirmationDialogService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly fieldsModel = signal<SettingsModel>(createSettingsModel());
  protected readonly fieldsForm = form(this.fieldsModel, applySettingsRules);
  protected readonly startDefaultsModel = signal<StartDefaultsModel>(createStartDefaultsModel());
  protected readonly startDefaultsForm = form(this.startDefaultsModel, applyStartDefaultsRules);
  protected readonly savedSettingsRequest = signal<UpdateCalendarEventDefaultsRequest | null>(null);
  protected readonly hasPendingSettingsChanges = computed(() => {
    const saved = this.savedSettingsRequest();
    if (saved === null) {
      return false;
    }

    const current = this.currentRequest();
    return (
      !sameUpdateEventTextFieldsRequest(current.eventTextFields, saved.eventTextFields) ||
      !sameUpdateStartDefaultsRequest(current.startDefaults, saved.startDefaults)
    );
  });
  protected readonly saveDisabled = computed(
    () => this.isLoadingSettings() || this.isSavingSettings() || !this.hasPendingSettingsChanges(),
  );

  protected readonly typeOptions: readonly SelectOption[] = [
    { value: 'ShortText', label: 'Short text' },
    { value: 'LongText', label: 'Long text' },
  ];
  protected readonly weekdayOptions = weekdayOptions;
  protected readonly startDefaultsTimeZoneOptions = startDefaultsTimeZoneOptions;

  protected readonly isLoadingSettings = signal(true);
  protected readonly showSettingsLoading = delayedLoading(() => this.isLoadingSettings());
  protected readonly settingsLoadFailed = signal(false);
  protected readonly isSavingSettings = signal(false);
  protected readonly settingsSaveErrorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadSettings();
  }

  canDeactivateWithPendingChanges(): boolean | Observable<boolean> {
    if (!this.hasPendingSettingsChanges() || this.hasActiveRequest()) {
      return true;
    }

    return this.confirmDiscardSettingsChanges();
  }

  protected addField(): void {
    this.fieldsModel.update(({ fields }) => ({ fields: appendEventTextField(fields) }));
  }

  protected deleteField(index: number): void {
    this.fieldsModel.update(({ fields }) => ({ fields: deleteEventTextField(fields, index) }));
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    this.saveSettings();
  }

  protected cancel(): void {
    if (this.hasActiveRequest()) {
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

  private saveSettings(): void {
    if (this.saveDisabled()) {
      return;
    }

    this.settingsSaveErrorMessage.set(null);
    if (this.fieldsForm().invalid() || this.startDefaultsForm().invalid()) {
      this.fieldsForm().markAsTouched();
      this.startDefaultsForm().markAsTouched();
      return;
    }

    this.isSavingSettings.set(true);
    this.defaults
      .update(this.currentRequest())
      .pipe(
        finalize(() => this.isSavingSettings.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.applySettings(response);
          this.notifications.showSuccess('Settings saved.');
        },
        error: () => this.settingsSaveErrorMessage.set('Settings could not be saved. Try again.'),
      });
  }

  private loadSettings(): void {
    this.isLoadingSettings.set(true);
    this.settingsLoadFailed.set(false);
    this.defaults
      .get()
      .pipe(
        finalize(() => this.isLoadingSettings.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => this.applySettings(response),
        error: () => {
          this.fieldsModel.set(createSettingsModel());
          this.startDefaultsModel.set(createStartDefaultsModel());
          this.savedSettingsRequest.set(null);
          this.settingsLoadFailed.set(true);
        },
      });
  }

  private applySettings(response: CalendarEventDefaultsResponse): void {
    this.fieldsModel.set(createSettingsModel(response.eventTextFields.fields));
    this.startDefaultsModel.set(createStartDefaultsModel(response.startDefaults));
    this.savedSettingsRequest.set(this.currentRequest());
    this.settingsSaveErrorMessage.set(null);
  }

  private restoreSavedSettings(): void {
    const saved = this.savedSettingsRequest();
    if (saved !== null) {
      this.fieldsModel.set(createSettingsModel(saved.eventTextFields.fields));
      this.startDefaultsModel.set(createStartDefaultsModel(saved.startDefaults));
    }
    this.settingsSaveErrorMessage.set(null);
  }

  private currentRequest(): UpdateCalendarEventDefaultsRequest {
    return {
      eventTextFields: toUpdateEventTextFieldsRequest(this.fieldsModel()),
      startDefaults: toUpdateStartDefaultsRequest(this.startDefaultsModel()),
    };
  }

  private confirmDiscardSettingsChanges(): Observable<boolean> {
    return this.confirmation
      .confirm<'keep-editing' | 'discard'>({
        kind: 'warning',
        title: 'Discard unsaved settings changes?',
        body: 'Event text field or new calendar event default changes have not been saved.',
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

  private hasActiveRequest(): boolean {
    return this.isLoadingSettings() || this.isSavingSettings();
  }
}
