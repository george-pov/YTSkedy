import { ChangeDetectionStrategy, Component, inject, signal, type OnInit } from '@angular/core';
import { applyEach, form, validate, type SchemaPathTree } from '@angular/forms/signals';
import { finalize } from 'rxjs';

import {
  EventTextField,
  EventTextFieldsService,
  EventTextType,
} from 'src/app/shared/api/settings/event-text-fields-service';
import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { Input } from 'src/app/shared/components/input/input';
import { delayedLoading } from 'src/app/shared/components/progress-bar/delayed-loading';
import { ProgressBar } from 'src/app/shared/components/progress-bar/progress-bar';
import { Select, SelectOption } from 'src/app/shared/components/select/select';
import { NotificationService } from 'src/app/shared/notifications/notification-service';

interface EventTextFieldEditor {
  fieldKey: string;
  label: string;
  type: string;
  maxLength: string;
}

interface SettingsModel {
  fields: EventTextFieldEditor[];
}

const defaultNewFieldType: EventTextType = 'ShortText';
const defaultNewFieldMaxLength = 50;

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

  protected readonly model = signal<SettingsModel>({ fields: [] });
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
      fields: renumberFields([
        ...fields,
        {
          fieldKey: '',
          label: `Text ${fields.length + 1}`,
          type: defaultNewFieldType,
          maxLength: defaultNewFieldMaxLength.toString(),
        },
      ]),
    }));
  }

  protected deleteField(index: number): void {
    const fields = this.model().fields;
    if (fields.length <= 1) {
      return;
    }

    this.model.set({
      fields: renumberFields(fields.filter((_, fieldIndex) => fieldIndex !== index)),
    });
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
      .update({ fields: this.model().fields.map(toEventTextField) })
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (response) => {
          this.model.set({ fields: response.fields.map(toEditorField) });
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
          this.model.set({ fields: response.fields.map(toEditorField) });
        },
        error: () => {
          this.model.set({ fields: [] });
          this.loadFailed.set(true);
        },
      });
  }
}

function applySettingsRules(path: SchemaPathTree<SettingsModel>): void {
  validate(path.fields, ({ value }) =>
    value().length === 0
      ? { kind: 'required', message: 'At least one event text field is required.' }
      : undefined,
  );

  applyEach(path.fields, (field) => {
    validate(field.label, ({ value }) =>
      value().trim().length === 0
        ? { kind: 'required', message: 'Label is required.' }
        : undefined,
    );

    validate(field.type, ({ value }) =>
      isEventTextType(value())
        ? undefined
        : { kind: 'required', message: 'Type is required.' },
    );

    validate(field.maxLength, ({ value }) => {
      const parsed = Number(value());
      if (!Number.isInteger(parsed) || parsed <= 0) {
        return {
          kind: 'min',
          message: 'Max length must be a positive whole number.',
        };
      }

      return undefined;
    });
  });
}

function toEditorField(field: EventTextField): EventTextFieldEditor {
  return {
    fieldKey: field.fieldKey,
    label: field.label,
    type: field.type,
    maxLength: field.maxLength.toString(),
  };
}

function toEventTextField(field: EventTextFieldEditor): EventTextField {
  return {
    fieldKey: field.fieldKey,
    label: field.label.trim(),
    type: field.type as EventTextType,
    maxLength: Number(field.maxLength),
  };
}

function renumberFields(fields: readonly EventTextFieldEditor[]): EventTextFieldEditor[] {
  return fields.map((field, index) => ({
    ...field,
    fieldKey: `text${index + 1}`,
  }));
}

function isEventTextType(value: string): value is EventTextType {
  return value === 'ShortText' || value === 'LongText';
}
