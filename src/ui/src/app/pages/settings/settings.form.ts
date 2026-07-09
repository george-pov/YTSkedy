import { applyEach, validate, type SchemaPathTree } from '@angular/forms/signals';

import {
  EventTextField,
  EventTextType,
  UpdateEventTextFieldsRequest,
} from 'src/app/shared/api/settings/event-text-fields-service';
import { sameRequest } from 'src/app/shared/forms/request-comparison';

export interface EventTextFieldEditor {
  fieldKey: string;
  label: string;
  type: string;
  maxLength: string;
}

export interface SettingsModel {
  fields: EventTextFieldEditor[];
}

const defaultNewFieldType: EventTextType = 'ShortText';
const defaultNewFieldMaxLength = 50;

export function createSettingsModel(fields: readonly EventTextField[] = []): SettingsModel {
  return {
    fields: fields.map(toEditorField),
  };
}

export function appendEventTextField(
  fields: readonly EventTextFieldEditor[],
): EventTextFieldEditor[] {
  return renumberFields([
    ...fields,
    {
      fieldKey: '',
      label: `Text ${fields.length + 1}`,
      type: defaultNewFieldType,
      maxLength: defaultNewFieldMaxLength.toString(),
    },
  ]);
}

export function deleteEventTextField(
  fields: readonly EventTextFieldEditor[],
  index: number,
): EventTextFieldEditor[] {
  if (fields.length <= 1) {
    return [...fields];
  }

  return renumberFields(fields.filter((_, fieldIndex) => fieldIndex !== index));
}

export function toUpdateEventTextFieldsRequest(model: SettingsModel): UpdateEventTextFieldsRequest {
  return {
    fields: model.fields.map(toEventTextField),
  };
}

export function sameUpdateEventTextFieldsRequest(
  left: UpdateEventTextFieldsRequest,
  right: UpdateEventTextFieldsRequest,
): boolean {
  return sameRequest(left, right);
}

export function applySettingsRules(path: SchemaPathTree<SettingsModel>): void {
  validate(path.fields, ({ value }) =>
    value().length === 0
      ? { kind: 'required', message: 'At least one event text field is required.' }
      : undefined,
  );

  applyEach(path.fields, (field) => {
    validate(field.label, ({ value }) =>
      value().trim().length === 0 ? { kind: 'required', message: 'Label is required.' } : undefined,
    );

    validate(field.type, ({ value }) =>
      isEventTextType(value()) ? undefined : { kind: 'required', message: 'Type is required.' },
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
