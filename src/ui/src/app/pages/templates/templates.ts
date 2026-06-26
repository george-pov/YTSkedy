import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, type OnInit, signal } from '@angular/core';
import { form } from '@angular/forms/signals';
import { finalize } from 'rxjs';

import { Template, TemplatesService } from 'src/app/shared/api/templates/templates-service';
import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { DataTable } from 'src/app/shared/components/data-table/data-table';
import { DataTableColumn } from 'src/app/shared/components/data-table/data-table-column';
import { Input } from 'src/app/shared/components/input/input';
import { delayedLoading } from 'src/app/shared/components/progress-bar/delayed-loading';
import { ProgressBar } from 'src/app/shared/components/progress-bar/progress-bar';
import { Select, SelectOption } from 'src/app/shared/components/select/select';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import {
  applyTemplateRules,
  createTemplateFormModel,
  TemplateFormModel,
  toCreateTemplateRequest,
  toUpdateTemplateRequest,
} from './templates.form';

// `none` hides the editor; `create` posts a new template on save; `edit` puts
// the selected template's name and content. The type is only editable while
// creating because the backend treats it as immutable after create.
type EditorMode = 'none' | 'create' | 'edit';

@Component({
  selector: 'app-templates',
  imports: [Alert, Button, DataTable, Input, ProgressBar, Select],
  templateUrl: './templates.html',
  styleUrl: './templates.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Templates implements OnInit {
  private readonly templatesService = inject(TemplatesService);
  private readonly notifications = inject(NotificationService);

  protected readonly templates = signal<Template[]>([]);
  protected readonly selected = signal<Template | null>(null);
  protected readonly editorMode = signal<EditorMode>('none');

  protected readonly isLoading = signal(true);
  protected readonly showLoading = delayedLoading(() => this.isLoading());
  protected readonly loadFailed = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isDeleting = signal(false);
  // Single editor-scoped error surface for a failed save or delete.
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly columns: readonly DataTableColumn<Template>[] = [
    { key: 'type', header: 'Type', value: (template) => template.type },
    { key: 'name', header: 'Name', value: (template) => template.name },
  ];

  protected readonly typeOptions: readonly SelectOption[] = [
    { value: 'YouTube', label: 'YouTube' },
    { value: 'WordPress', label: 'WordPress' },
  ];

  protected readonly model = signal<TemplateFormModel>(createTemplateFormModel());
  protected readonly form = form(this.model, applyTemplateRules);

  ngOnInit(): void {
    this.loadTemplates();
  }

  protected select(id: string): void {
    const template = this.templates().find((entry) => entry.id === id);
    if (template === undefined) {
      return;
    }

    this.errorMessage.set(null);
    this.selected.set(template);
    this.editorMode.set('edit');
    this.model.set({
      type: template.type,
      name: template.name,
      content: template.content,
    });
  }

  protected newTemplate(): void {
    this.errorMessage.set(null);
    this.selected.set(null);
    this.editorMode.set('create');
    this.model.set(createTemplateFormModel());
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    this.save();
  }

  protected save(): void {
    if (this.isSaving() || this.isDeleting() || this.editorMode() === 'none') {
      return;
    }

    if (this.form().invalid()) {
      this.form().markAsTouched();
      return;
    }

    if (this.editorMode() === 'create') {
      this.createTemplate();
    } else {
      this.updateTemplate();
    }
  }

  protected deleteSelected(): void {
    const current = this.selected();
    if (current === null || this.isSaving() || this.isDeleting()) {
      return;
    }

    this.errorMessage.set(null);
    this.isDeleting.set(true);

    this.templatesService
      .delete(current.type, current.id)
      .pipe(finalize(() => this.isDeleting.set(false)))
      .subscribe({
        next: () => {
          this.removeFromList(current.id);
          this.notifications.showSuccess('Template deleted.');
        },
        error: (error: unknown) => {
          // A 404 means the row is already gone; treat that as completed
          // cleanup and close the editor. Anything else keeps the operator here
          // with an explanation.
          if (error instanceof HttpErrorResponse && error.status === 404) {
            this.removeFromList(current.id);
            this.notifications.showSuccess('Template no longer exists.');
            return;
          }

          this.errorMessage.set(describeDeleteError(error));
        },
      });
  }

  private loadTemplates(): void {
    this.isLoading.set(true);
    this.loadFailed.set(false);

    this.templatesService
      .list()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (response) => {
          this.templates.set(sortTemplates(response.templates));
        },
        error: () => {
          this.templates.set([]);
          this.loadFailed.set(true);
        },
      });
  }

  private createTemplate(): void {
    const request = toCreateTemplateRequest(this.model());
    const content = request.content;

    this.errorMessage.set(null);
    this.isSaving.set(true);

    this.templatesService
      .create(request)
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (response) => {
          // The create response omits content, so keep the submitted content
          // for the in-place row and the now-editing form.
          const created: Template = {
            id: response.id,
            name: response.name,
            type: response.type,
            content,
          };
          this.templates.update((list) => sortTemplates([created, ...list]));
          this.selected.set(created);
          this.editorMode.set('edit');
          this.notifications.showSuccess('Template created.');
        },
        error: (error: unknown) => {
          this.errorMessage.set(describeSaveError(error));
        },
      });
  }

  private updateTemplate(): void {
    const current = this.selected();
    if (current === null) {
      return;
    }

    const request = toUpdateTemplateRequest(this.model());

    this.errorMessage.set(null);
    this.isSaving.set(true);

    // The type is immutable, so the original type and id locate the row.
    this.templatesService
      .update(current.type, current.id, request)
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (response) => {
          const updated: Template = {
            id: current.id,
            type: current.type,
            name: response.name,
            content: request.content,
          };
          this.templates.update((list) =>
            sortTemplates(list.map((entry) => (entry.id === current.id ? updated : entry))),
          );
          this.selected.set(updated);
          this.notifications.showSuccess('Template saved.');
        },
        error: (error: unknown) => {
          this.errorMessage.set(describeSaveError(error));
        },
      });
  }

  private removeFromList(id: string): void {
    this.templates.update((list) => list.filter((entry) => entry.id !== id));
    this.selected.set(null);
    this.editorMode.set('none');
  }
}

// The list endpoint order is not significant, so sort client-side by type then
// name for a stable, predictable display.
function sortTemplates(templates: readonly Template[]): Template[] {
  return [...templates].sort((left, right) => {
    const byType = left.type.localeCompare(right.type);
    return byType !== 0 ? byType : left.name.localeCompare(right.name);
  });
}

// A 409 means the name is already used within the type; a 403 means the
// signed-in user lacks the required scope or role. Anything else is treated as
// a transient or connection failure.
function describeSaveError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 409) {
      return 'A template with this name already exists for this type.';
    }
    if (error.status === 403) {
      return 'You do not have permission to manage templates.';
    }
  }

  return 'The template could not be saved. Check your connection and try again.';
}

function describeDeleteError(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 403) {
    return 'You do not have permission to manage templates.';
  }

  return 'The template could not be deleted. Check your connection and try again.';
}
