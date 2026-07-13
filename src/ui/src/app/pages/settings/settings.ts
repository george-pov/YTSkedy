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
  CalendarEventStartDefaultsResponse,
  CalendarEventStartDefaultsService,
  UpdateCalendarEventStartDefaultsRequest,
} from 'src/app/shared/api/settings/calendar-event-start-defaults-service';
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
  private readonly eventTextFields = inject(EventTextFieldsService);
  private readonly startDefaultsService = inject(CalendarEventStartDefaultsService);
  private readonly confirmation = inject(ConfirmationDialogService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly fieldsModel = signal<SettingsModel>(createSettingsModel());
  protected readonly fieldsForm = form(this.fieldsModel, applySettingsRules);
  protected readonly savedFieldsRequest = signal<UpdateEventTextFieldsRequest | null>(null);
  protected readonly hasPendingEventTextChanges = computed(() => {
    const saved = this.savedFieldsRequest();
    return (
      saved !== null &&
      !sameUpdateEventTextFieldsRequest(toUpdateEventTextFieldsRequest(this.fieldsModel()), saved)
    );
  });
  protected readonly eventTextSaveDisabled = computed(
    () =>
      this.isLoadingFields() ||
      this.isSavingFields() ||
      !this.hasPendingEventTextChanges(),
  );

  protected readonly startDefaultsModel = signal<StartDefaultsModel>(createStartDefaultsModel());
  protected readonly startDefaultsForm = form(this.startDefaultsModel, applyStartDefaultsRules);
  protected readonly savedStartDefaultsRequest =
    signal<UpdateCalendarEventStartDefaultsRequest | null>(null);
  protected readonly hasPendingStartDefaultsChanges = computed(() => {
    const saved = this.savedStartDefaultsRequest();
    return (
      saved !== null &&
      !sameUpdateStartDefaultsRequest(toUpdateStartDefaultsRequest(this.startDefaultsModel()), saved)
    );
  });
  protected readonly startDefaultsSaveDisabled = computed(
    () =>
      this.isLoadingStartDefaults() ||
      this.isSavingStartDefaults() ||
      !this.hasPendingStartDefaultsChanges(),
  );
  protected readonly hasPendingSettingsChanges = computed(
    () => this.hasPendingEventTextChanges() || this.hasPendingStartDefaultsChanges(),
  );

  protected readonly typeOptions: readonly SelectOption[] = [
    { value: 'ShortText', label: 'Short text' },
    { value: 'LongText', label: 'Long text' },
  ];
  protected readonly weekdayOptions = weekdayOptions;
  protected readonly startDefaultsTimeZoneOptions = startDefaultsTimeZoneOptions;

  protected readonly isLoadingFields = signal(true);
  protected readonly showFieldsLoading = delayedLoading(() => this.isLoadingFields());
  protected readonly fieldsLoadFailed = signal(false);
  protected readonly isSavingFields = signal(false);
  protected readonly fieldsSaveErrorMessage = signal<string | null>(null);

  protected readonly isLoadingStartDefaults = signal(true);
  protected readonly showStartDefaultsLoading = delayedLoading(() => this.isLoadingStartDefaults());
  protected readonly startDefaultsLoadFailed = signal(false);
  protected readonly isSavingStartDefaults = signal(false);
  protected readonly startDefaultsSaveErrorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadFields();
    this.loadStartDefaults();
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

  protected onEventTextSubmit(event: Event): void {
    event.preventDefault();
    this.saveEventTextFields();
  }

  protected onStartDefaultsSubmit(event: Event): void {
    event.preventDefault();
    this.saveStartDefaults();
  }

  protected cancel(): void {
    if (this.hasActiveRequest()) {
      return;
    }

    if (!this.hasPendingSettingsChanges()) {
      this.restoreSavedFields();
      this.restoreSavedStartDefaults();
      return;
    }

    this.confirmDiscardSettingsChanges().subscribe((discard) => {
      if (discard) {
        this.restoreSavedFields();
        this.restoreSavedStartDefaults();
      }
    });
  }

  protected saveEventTextFields(): void {
    if (this.eventTextSaveDisabled()) {
      return;
    }

    this.fieldsSaveErrorMessage.set(null);
    if (this.fieldsForm().invalid()) {
      this.fieldsForm().markAsTouched();
      return;
    }

    this.isSavingFields.set(true);
    this.eventTextFields
      .update(toUpdateEventTextFieldsRequest(this.fieldsModel()))
      .pipe(
        finalize(() => this.isSavingFields.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.applyFields(response.fields);
          this.notifications.showSuccess('Event text fields saved.');
        },
        error: () =>
          this.fieldsSaveErrorMessage.set('Event text fields could not be saved. Try again.'),
      });
  }

  protected saveStartDefaults(): void {
    if (this.startDefaultsSaveDisabled()) {
      return;
    }

    this.startDefaultsSaveErrorMessage.set(null);
    this.isSavingStartDefaults.set(true);
    this.startDefaultsService
      .update(toUpdateStartDefaultsRequest(this.startDefaultsModel()))
      .pipe(
        finalize(() => this.isSavingStartDefaults.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.applyStartDefaults(response);
          this.notifications.showSuccess('New calendar event defaults saved.');
        },
        error: () =>
          this.startDefaultsSaveErrorMessage.set(
            'New calendar event defaults could not be saved. Try again.',
          ),
      });
  }

  private loadFields(): void {
    this.isLoadingFields.set(true);
    this.fieldsLoadFailed.set(false);
    this.eventTextFields
      .get()
      .pipe(
        finalize(() => this.isLoadingFields.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => this.applyFields(response.fields),
        error: () => {
          this.fieldsModel.set(createSettingsModel());
          this.savedFieldsRequest.set(null);
          this.fieldsLoadFailed.set(true);
        },
      });
  }

  private loadStartDefaults(): void {
    this.isLoadingStartDefaults.set(true);
    this.startDefaultsLoadFailed.set(false);
    this.startDefaultsService
      .get()
      .pipe(
        finalize(() => this.isLoadingStartDefaults.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => this.applyStartDefaults(response),
        error: () => {
          this.startDefaultsModel.set(createStartDefaultsModel());
          this.savedStartDefaultsRequest.set(null);
          this.startDefaultsLoadFailed.set(true);
        },
      });
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

  private applyFields(fields: readonly EventTextField[]): void {
    this.fieldsModel.set(createSettingsModel(fields));
    this.savedFieldsRequest.set(toUpdateEventTextFieldsRequest(this.fieldsModel()));
    this.fieldsSaveErrorMessage.set(null);
  }

  private applyStartDefaults(response: CalendarEventStartDefaultsResponse): void {
    this.startDefaultsModel.set(createStartDefaultsModel(response));
    this.savedStartDefaultsRequest.set(toUpdateStartDefaultsRequest(this.startDefaultsModel()));
    this.startDefaultsSaveErrorMessage.set(null);
  }

  private restoreSavedFields(): void {
    const saved = this.savedFieldsRequest();
    if (saved !== null) {
      this.fieldsModel.set(createSettingsModel(saved.fields));
    }
    this.fieldsSaveErrorMessage.set(null);
  }

  private restoreSavedStartDefaults(): void {
    const saved = this.savedStartDefaultsRequest();
    if (saved !== null) {
      this.startDefaultsModel.set(createStartDefaultsModel(saved));
    }
    this.startDefaultsSaveErrorMessage.set(null);
  }

  private hasActiveRequest(): boolean {
    return (
      this.isLoadingFields() ||
      this.isLoadingStartDefaults() ||
      this.isSavingFields() ||
      this.isSavingStartDefaults()
    );
  }
}
