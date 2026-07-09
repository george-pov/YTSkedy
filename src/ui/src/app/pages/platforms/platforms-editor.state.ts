import { computed, signal } from '@angular/core';
import { form } from '@angular/forms/signals';

import {
  CreatePlatformRequest,
  Platform,
  UpdatePlatformRequest,
} from 'src/app/shared/api/platforms/platforms-service';
import {
  applyPlatformRules,
  createPlatformFormModel,
  sameCreatePlatformRequest,
  sameUpdatePlatformRequest,
  toCreatePlatformRequest,
  toPlatformFormModel,
  toUpdatePlatformRequest,
} from './platforms.form';

// `none` hides the editor; `create` adds a new platform on save; `edit` puts
// the selected platform. The type is only editable while creating because it is
// immutable after create.
export type EditorMode = 'none' | 'create' | 'edit';

export class PlatformsEditorState {
  private readonly _platforms = signal<Platform[]>([]);
  private readonly _selected = signal<Platform | null>(null);
  private readonly _editorMode = signal<EditorMode>('none');
  private readonly createPlatformBaseline = signal<CreatePlatformRequest | null>(null);
  private readonly updatePlatformBaseline = signal<UpdatePlatformRequest | null>(null);
  private readonly _isSaving = signal(false);
  private readonly _isDeleting = signal(false);
  private readonly _errorMessage = signal<string | null>(null);

  readonly model = signal(createPlatformFormModel());
  readonly form = form(this.model, applyPlatformRules);
  readonly platforms = this._platforms.asReadonly();
  readonly selected = this._selected.asReadonly();
  readonly editorMode = this._editorMode.asReadonly();
  readonly isSaving = this._isSaving.asReadonly();
  readonly isDeleting = this._isDeleting.asReadonly();
  readonly errorMessage = this._errorMessage.asReadonly();
  readonly selectedType = computed(() => this.model().type);
  readonly hasActiveMutation = computed(() => this._isSaving() || this._isDeleting());
  readonly hasPendingPlatformChanges = computed(() => {
    if (this._editorMode() === 'create') {
      const baseline = this.createPlatformBaseline();
      return (
        baseline !== null &&
        !sameCreatePlatformRequest(toCreatePlatformRequest(this.model()), baseline)
      );
    }

    const baseline = this.updatePlatformBaseline();
    return (
      this._editorMode() === 'edit' &&
      baseline !== null &&
      !sameUpdatePlatformRequest(toUpdatePlatformRequest(this.model()), baseline)
    );
  });
  readonly saveDisabled = computed(
    () =>
      this.hasActiveMutation() ||
      this._editorMode() === 'none' ||
      !this.hasPendingPlatformChanges(),
  );

  applyLoadedPlatforms(platforms: readonly Platform[]): void {
    const sorted = sortPlatforms(platforms);
    this._platforms.set(sorted);

    if (this._editorMode() === 'none' && sorted.length > 0) {
      this.openPlatform(sorted[0]);
    }
  }

  resetAfterLoadFailure(): void {
    this._platforms.set([]);
  }

  openPlatform(platform: Platform): void {
    const model = toPlatformFormModel(platform);

    this._errorMessage.set(null);
    this._selected.set(platform);
    this._editorMode.set('edit');
    this.model.set(model);
    this.createPlatformBaseline.set(null);
    this.updatePlatformBaseline.set(toUpdatePlatformRequest(model));
  }

  openNewPlatform(): void {
    const model = createPlatformFormModel();

    this._errorMessage.set(null);
    this._selected.set(null);
    this._editorMode.set('create');
    this.model.set(model);
    this.createPlatformBaseline.set(toCreatePlatformRequest(model));
    this.updatePlatformBaseline.set(null);
  }

  closeEditor(): void {
    this._selected.set(null);
    this._editorMode.set('none');
    this.model.set(createPlatformFormModel());
    this.createPlatformBaseline.set(null);
    this.updatePlatformBaseline.set(null);
    this._errorMessage.set(null);
  }

  applyCreatedPlatform(platform: Platform): void {
    this._platforms.update((list) => sortPlatforms([platform, ...list]));
    this.applySavedPlatform(platform);
  }

  applyUpdatedPlatform(platform: Platform): void {
    const selectedId = this._selected()?.id ?? platform.id;
    this._platforms.update((list) =>
      sortPlatforms(list.map((entry) => (entry.id === selectedId ? platform : entry))),
    );
    this.applySavedPlatform(platform);
  }

  removeDeletedPlatform(id: string): void {
    this._platforms.update((list) => list.filter((entry) => entry.id !== id));
    this.closeEditor();
  }

  clearUnavailableTemplateIds(templates: readonly { id: string }[]): void {
    if (this._editorMode() !== 'create') {
      return;
    }

    const model = this.model();
    const availableIds = new Set(templates.map((template) => template.id));
    const titleTemplateId = availableIds.has(model.titleTemplateId) ? model.titleTemplateId : '';
    const descriptionTemplateId = availableIds.has(model.descriptionTemplateId)
      ? model.descriptionTemplateId
      : '';

    if (
      titleTemplateId !== model.titleTemplateId ||
      descriptionTemplateId !== model.descriptionTemplateId
    ) {
      this.model.set({ ...model, titleTemplateId, descriptionTemplateId });
    }
  }

  setSaving(isSaving: boolean): void {
    this._isSaving.set(isSaving);
  }

  setDeleting(isDeleting: boolean): void {
    this._isDeleting.set(isDeleting);
  }

  setErrorMessage(message: string | null): void {
    this._errorMessage.set(message);
  }

  private applySavedPlatform(platform: Platform): void {
    const model = toPlatformFormModel(platform);

    this._selected.set(platform);
    this._editorMode.set('edit');
    this.model.set(model);
    this.createPlatformBaseline.set(null);
    this.updatePlatformBaseline.set(toUpdatePlatformRequest(model));
  }
}

// The list order is not significant, so sort client-side by type then name for
// a stable, predictable display.
function sortPlatforms(platforms: readonly Platform[]): Platform[] {
  return [...platforms].sort((left, right) => {
    const byType = left.type.localeCompare(right.type);
    return byType !== 0 ? byType : left.name.localeCompare(right.name);
  });
}
