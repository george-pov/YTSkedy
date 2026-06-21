import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { form } from '@angular/forms/signals';

import { Button } from 'src/app/shared/components/button/button';
import { DataTable } from 'src/app/shared/components/data-table/data-table';
import { DataTableColumn } from 'src/app/shared/components/data-table/data-table-column';
import { Input } from 'src/app/shared/components/input/input';
import {
  applyTemplateRules,
  createTemplateFormModel,
  Template,
  TemplateFormModel,
} from './templates.form';

// Hardcoded in-memory data for now; a templates API and persistence come later.
const INITIAL_TEMPLATES: readonly Template[] = [
  {
    id: 1,
    type: 'YouTube',
    name: 'Weekly live stream',
    content: 'LIVE: {{title}}\n\nJoin us {{date}} at {{time}}!\n\n{{description}}',
  },
  {
    id: 2,
    type: 'WordPress',
    name: 'New blog post',
    content: '<h1>{{title}}</h1>\n<p>Published {{date}}</p>\n\n{{content}}',
  },
  {
    id: 3,
    type: 'Facebook',
    name: 'Event announcement',
    content: '{{title}}\n\n{{description}}\n\nWhen: {{date}}',
  },
];

@Component({
  selector: 'app-templates',
  imports: [Button, DataTable, Input],
  templateUrl: './templates.html',
  styleUrl: './templates.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Templates {
  protected readonly templates = signal<Template[]>(
    INITIAL_TEMPLATES.map((template) => ({ ...template })),
  );
  protected readonly selectedId = signal<number | null>(null);

  protected readonly selectedTemplate = computed(
    () =>
      this.templates().find((template) => template.id === this.selectedId()) ??
      null,
  );

  protected readonly columns: readonly DataTableColumn<Template>[] = [
    { key: 'type', header: 'Type', value: (template) => template.type },
    { key: 'name', header: 'Name', value: (template) => template.name },
  ];

  protected readonly model = signal<TemplateFormModel>(
    createTemplateFormModel(),
  );
  protected readonly form = form(this.model, applyTemplateRules);

  private nextId = INITIAL_TEMPLATES.length + 1;

  protected select(id: number): void {
    const template = this.templates().find((entry) => entry.id === id);
    if (template === undefined) {
      return;
    }

    this.selectedId.set(id);
    this.model.set({
      type: template.type,
      name: template.name,
      content: template.content,
    });
  }

  protected newTemplate(): void {
    const created: Template = {
      id: this.nextId++,
      type: 'YouTube',
      name: 'New template',
      content: '',
    };
    this.templates.update((list) => [created, ...list]);
    this.select(created.id);
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    this.save();
  }

  protected save(): void {
    const id = this.selectedId();
    if (id === null) {
      return;
    }

    if (this.form().invalid()) {
      this.form().markAsTouched();
      return;
    }

    const value = this.model();
    this.templates.update((list) =>
      list.map((entry) =>
        entry.id === id ? { ...entry, ...value } : entry,
      ),
    );
  }

  protected deleteSelected(): void {
    const id = this.selectedId();
    if (id === null) {
      return;
    }

    this.templates.update((list) => list.filter((entry) => entry.id !== id));
    this.selectedId.set(null);
  }
}

