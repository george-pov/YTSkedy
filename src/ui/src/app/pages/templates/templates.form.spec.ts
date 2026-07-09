import { describe, expect, it } from 'vitest';

import {
  sameTemplateEditorRequest,
  toTemplateEditorRequest,
  type TemplateFormModel,
} from './templates.form';

describe('templates form dirty-state mapping', () => {
  it('normalizes trimmed name changes before comparison', () => {
    const saved = toTemplateEditorRequest(validModel({ name: 'Weeknight stream' }));
    const edited = toTemplateEditorRequest(validModel({ name: '  Weeknight stream  ' }));

    expect(sameTemplateEditorRequest(edited, saved)).toBe(true);
  });

  it('keeps content changes significant without trimming content', () => {
    const saved = toTemplateEditorRequest(validModel({ content: 'Live at 8' }));
    const edited = toTemplateEditorRequest(validModel({ content: 'Live at 8 ' }));

    expect(sameTemplateEditorRequest(edited, saved)).toBe(false);
  });

  function validModel(overrides: Partial<TemplateFormModel>): TemplateFormModel {
    return {
      type: 'YouTube',
      name: 'Weeknight stream',
      content: 'Live at 8',
      ...overrides,
    };
  }
});
