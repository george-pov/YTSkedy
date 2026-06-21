import {
  maxLength,
  required,
  validate,
  type SchemaPathTree,
} from '@angular/forms/signals';

import {
  CreateTemplateRequest,
  TemplateType,
  UpdateTemplateRequest,
} from 'src/app/shared/api/templates/templates-service';

// Mirror the backend domain limits in `Template.cs` so the UI rejects oversized
// input before the request. The backend remains the durable validator.
export const nameMaxLength = 50;
export const contentMaxLength = 2000;

/** Editable fields of a template surfaced in the editor form. */
export interface TemplateFormModel {
  type: string;
  name: string;
  content: string;
}

// New templates default to YouTube so the type select starts on a valid option.
export function createTemplateFormModel(): TemplateFormModel {
  return { type: 'YouTube', name: '', content: '' };
}

// Signal Forms validation rules for the template editor. Type is required (the
// select always holds a value); name and content are required-trimmed to reject
// whitespace-only input like the backend, and both enforce the backend length
// limits.
export function applyTemplateRules(path: SchemaPathTree<TemplateFormModel>): void {
  required(path.type, { message: 'Type is required.' });

  validate(path.name, ({ value }) =>
    value().trim().length === 0
      ? { kind: 'required', message: 'Name is required.' }
      : undefined,
  );
  maxLength(path.name, nameMaxLength, {
    message: `Name must be at most ${nameMaxLength} characters.`,
  });

  validate(path.content, ({ value }) =>
    value().trim().length === 0
      ? { kind: 'required', message: 'Content is required.' }
      : undefined,
  );
  maxLength(path.content, contentMaxLength, {
    message: `Content must be at most ${contentMaxLength} characters.`,
  });
}

// Pure mappings from the editor model to the API request shapes. Create carries
// the type; update omits it because the backend treats type as immutable and
// reads it from the route.
export function toCreateTemplateRequest(model: TemplateFormModel): CreateTemplateRequest {
  return {
    name: model.name.trim(),
    type: model.type as TemplateType,
    content: model.content,
  };
}

export function toUpdateTemplateRequest(model: TemplateFormModel): UpdateTemplateRequest {
  return {
    name: model.name.trim(),
    content: model.content,
  };
}
