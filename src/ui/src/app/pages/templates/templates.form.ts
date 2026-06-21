import { required, type SchemaPathTree } from '@angular/forms/signals';

/**
 * A reusable template for formatting a post on a social platform. `type` is the
 * target platform (for example YouTube, WordPress, Facebook), `name` is the
 * operator-facing label, and `content` is the post body with `{{placeholder}}`
 * tokens filled in at publish time.
 */
export interface Template {
  id: number;
  type: string;
  name: string;
  content: string;
}

/** Editable fields of a {@link Template} (everything except its identity). */
export interface TemplateFormModel {
  type: string;
  name: string;
  content: string;
}

export function createTemplateFormModel(): TemplateFormModel {
  return { type: '', name: '', content: '' };
}

// Signal Forms validation rules for the template editor. Name and type are
// required; content may be empty while drafting.
export function applyTemplateRules(
  path: SchemaPathTree<TemplateFormModel>,
): void {
  required(path.name, { message: 'Name is required.' });
  required(path.type, { message: 'Type is required.' });
}
