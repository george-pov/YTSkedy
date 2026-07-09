import { describe, expect, it } from 'vitest';

import { EventTextField } from 'src/app/shared/api/settings/event-text-fields-service';
import {
  appendEventTextField,
  createSettingsModel,
  deleteEventTextField,
  sameUpdateEventTextFieldsRequest,
  toUpdateEventTextFieldsRequest,
  type EventTextFieldEditor,
} from './settings.form';

describe('settings form mapping', () => {
  it('maps API fields to editable string values', () => {
    expect(createSettingsModel(eventTextFields()).fields).toEqual([
      {
        fieldKey: 'text1',
        label: 'Title',
        type: 'ShortText',
        maxLength: '50',
      },
      {
        fieldKey: 'text2',
        label: 'Description',
        type: 'LongText',
        maxLength: '2500',
      },
    ]);
  });

  it('appends a field with the next key and default editor values', () => {
    expect(appendEventTextField(editorFields())).toEqual([
      ...editorFields(),
      {
        fieldKey: 'text3',
        label: 'Text 3',
        type: 'ShortText',
        maxLength: '50',
      },
    ]);
  });

  it('deletes a field and renumbers the remaining fields', () => {
    expect(
      deleteEventTextField(
        [
          ...editorFields(),
          {
            fieldKey: 'text3',
            label: 'Description',
            type: 'LongText',
            maxLength: '2500',
          },
        ],
        1,
      ),
    ).toEqual([
      {
        fieldKey: 'text1',
        label: 'Title',
        type: 'ShortText',
        maxLength: '50',
      },
      {
        fieldKey: 'text2',
        label: 'Description',
        type: 'LongText',
        maxLength: '2500',
      },
    ]);
  });

  it('keeps the last field when delete is requested', () => {
    expect(deleteEventTextField([editorFields()[0]], 0)).toEqual([editorFields()[0]]);
  });

  it('maps editor fields to a trimmed update request', () => {
    expect(
      toUpdateEventTextFieldsRequest({
        fields: [
          {
            fieldKey: 'text1',
            label: ' Stream title ',
            type: 'ShortText',
            maxLength: '80',
          },
        ],
      }),
    ).toEqual({
      fields: [
        {
          fieldKey: 'text1',
          label: 'Stream title',
          type: 'ShortText',
          maxLength: 80,
        },
      ],
    });
  });

  it('normalizes trimmed label changes before comparison', () => {
    const saved = toUpdateEventTextFieldsRequest(createSettingsModel(eventTextFields()));
    const edited = toUpdateEventTextFieldsRequest({
      fields: [
        {
          fieldKey: 'text1',
          label: '  Title  ',
          type: 'ShortText',
          maxLength: '50',
        },
        {
          fieldKey: 'text2',
          label: '  Description  ',
          type: 'LongText',
          maxLength: '2500',
        },
      ],
    });

    expect(sameUpdateEventTextFieldsRequest(edited, saved)).toBe(true);
  });

  it('compares add, delete, renumber, type, and max length changes as dirty', () => {
    const saved = toUpdateEventTextFieldsRequest(createSettingsModel(eventTextFields()));

    expect(
      sameUpdateEventTextFieldsRequest(
        toUpdateEventTextFieldsRequest({
          fields: appendEventTextField(editorFields()),
        }),
        saved,
      ),
    ).toBe(false);
    expect(
      sameUpdateEventTextFieldsRequest(
        toUpdateEventTextFieldsRequest({
          fields: deleteEventTextField(editorFields(), 0),
        }),
        saved,
      ),
    ).toBe(false);
    expect(
      sameUpdateEventTextFieldsRequest(
        toUpdateEventTextFieldsRequest({
          fields: [
            {
              fieldKey: 'text2',
              label: 'Title',
              type: 'ShortText',
              maxLength: '50',
            },
            {
              fieldKey: 'text1',
              label: 'Description',
              type: 'LongText',
              maxLength: '2500',
            },
          ],
        }),
        saved,
      ),
    ).toBe(false);
    expect(
      sameUpdateEventTextFieldsRequest(
        toUpdateEventTextFieldsRequest({
          fields: [
            {
              ...editorFields()[0],
              type: 'LongText',
            },
            editorFields()[1],
          ],
        }),
        saved,
      ),
    ).toBe(false);
    expect(
      sameUpdateEventTextFieldsRequest(
        toUpdateEventTextFieldsRequest({
          fields: [
            {
              ...editorFields()[0],
              maxLength: '80',
            },
            editorFields()[1],
          ],
        }),
        saved,
      ),
    ).toBe(false);
  });

  function eventTextFields(): EventTextField[] {
    return [
      {
        fieldKey: 'text1',
        label: 'Title',
        type: 'ShortText',
        maxLength: 50,
      },
      {
        fieldKey: 'text2',
        label: 'Description',
        type: 'LongText',
        maxLength: 2500,
      },
    ];
  }

  function editorFields(): EventTextFieldEditor[] {
    return createSettingsModel(eventTextFields()).fields;
  }
});
