import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  type OnInit,
  signal,
} from '@angular/core';
import { form } from '@angular/forms/signals';
import { finalize, map, Observable } from 'rxjs';

import {
  CreatePlatformRequest,
  Platform,
  PlatformNameConflictError,
  PlatformReferenceKeyConflictError,
  PlatformsService,
  UpdatePlatformRequest,
  YouTubePublishSettings,
  WordPressPublishSettings,
} from 'src/app/shared/api/platforms/platforms-service';
import {
  Template,
  TemplatesService,
} from 'src/app/shared/api/templates/templates-service';
import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { ConfirmationDialogService } from 'src/app/shared/components/confirmation-dialog/confirmation-dialog-service';
import { DataTable } from 'src/app/shared/components/data-table/data-table';
import { DataTableColumn } from 'src/app/shared/components/data-table/data-table-column';
import { Input } from 'src/app/shared/components/input/input';
import { delayedLoading } from 'src/app/shared/components/progress-bar/delayed-loading';
import { ProgressBar } from 'src/app/shared/components/progress-bar/progress-bar';
import { Select, SelectOption } from 'src/app/shared/components/select/select';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { type PendingChangesAware } from 'src/app/shared/routing/pending-changes-guard';
import {
  applyPlatformRules,
  createPlatformFormModel,
  PlatformFormModel,
  sameCreatePlatformRequest,
  sameUpdatePlatformRequest,
  toCreatePlatformRequest,
  toUpdatePlatformRequest,
} from './platforms.form';
import { YouTubeSettings } from './youtube-settings/youtube-settings';
import { WordPressSettings } from './wordpress-settings/wordpress-settings';

// `none` hides the editor; `create` adds a new platform on save; `edit` puts
// the selected platform. The type is only editable while creating because it is
// immutable after create.
type EditorMode = 'none' | 'create' | 'edit';

@Component({
  selector: 'app-platforms',
  imports: [
    Alert,
    Button,
    DataTable,
    Input,
    ProgressBar,
    Select,
    YouTubeSettings,
    WordPressSettings,
  ],
  templateUrl: './platforms.html',
  styleUrl: './platforms.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Platforms implements OnInit, PendingChangesAware {
  private readonly platformsService = inject(PlatformsService);
  private readonly templatesService = inject(TemplatesService);
  private readonly confirmation = inject(ConfirmationDialogService);
  private readonly notifications = inject(NotificationService);
  private latestTemplateLoadId = 0;
  private loadedTemplateType: Platform['type'] | null = null;

  protected readonly platforms = signal<Platform[]>([]);
  protected readonly selected = signal<Platform | null>(null);
  protected readonly editorMode = signal<EditorMode>('none');
  protected readonly availableTemplates = signal<Template[]>([]);
  protected readonly templateLoadFailed = signal(false);

  protected readonly isLoading = signal(true);
  protected readonly showLoading = delayedLoading(() => this.isLoading());
  protected readonly loadFailed = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isDeleting = signal(false);
  // Single editor-scoped error surface for a failed save or delete.
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly columns: readonly DataTableColumn<Platform>[] = [
    { key: 'type', header: 'Type', value: (platform) => platform.type },
    { key: 'name', header: 'Name', value: (platform) => platform.name },
    {
      key: 'referenceKey',
      header: 'Reference key',
      value: (platform) => platform.referenceKey ?? '',
    },
  ];

  protected readonly typeOptions: readonly SelectOption[] = [
    { value: 'YouTube', label: 'YouTube' },
    { value: 'WordPress', label: 'WordPress' },
  ];

  protected readonly model = signal<PlatformFormModel>(createPlatformFormModel());
  protected readonly form = form(this.model, applyPlatformRules);
  protected readonly createPlatformBaseline = signal<CreatePlatformRequest | null>(null);
  protected readonly updatePlatformBaseline = signal<UpdatePlatformRequest | null>(null);

  // The type currently in the editor. Reactive to the create-mode type select,
  // so the settings section can switch on it.
  protected readonly selectedType = computed(() => this.model().type);
  protected readonly hasPendingPlatformChanges = computed(() => {
    if (this.editorMode() === 'create') {
      const baseline = this.createPlatformBaseline();
      return (
        baseline !== null &&
        !sameCreatePlatformRequest(toCreatePlatformRequest(this.model()), baseline)
      );
    }

    const baseline = this.updatePlatformBaseline();
    return (
      this.editorMode() === 'edit' &&
      baseline !== null &&
      !sameUpdatePlatformRequest(toUpdatePlatformRequest(this.model()), baseline)
    );
  });
  protected readonly saveDisabled = computed(
    () =>
      this.isSaving() ||
      this.isDeleting() ||
      this.editorMode() === 'none' ||
      !this.hasPendingPlatformChanges(),
  );
  protected readonly templateOptions = computed<readonly SelectOption[]>(() =>
    this.availableTemplates().map((template) => ({
      value: template.id,
      label: template.name,
    })),
  );

  constructor() {
    effect(() => {
      const type = this.selectedType();
      if (this.editorMode() === 'none' || !isPlatformType(type)) {
        return;
      }

      if (this.loadedTemplateType === type) {
        return;
      }

      this.loadedTemplateType = type;
      this.loadTemplates(type);
    });
  }

  ngOnInit(): void {
    this.loadPlatforms();
  }

  canDeactivateWithPendingChanges(): boolean | Observable<boolean> {
    if (!this.hasPendingPlatformChanges() || this.isSaving() || this.isDeleting()) {
      return true;
    }

    return this.confirmDiscardPlatformChanges();
  }

  protected select(id: string): void {
    if (this.editorMode() === 'edit' && this.selected()?.id === id) {
      return;
    }

    const platform = this.platforms().find((entry) => entry.id === id);
    if (platform === undefined) {
      return;
    }

    this.discardPlatformChangesBefore(() => this.openPlatform(platform));
  }

  protected newPlatform(): void {
    this.discardPlatformChangesBefore(() => this.openNewPlatform());
  }

  protected cancel(): void {
    if (this.isSaving() || this.isDeleting() || this.editorMode() === 'none') {
      return;
    }

    this.discardPlatformChangesBefore(() => this.closeEditor());
  }

  private openPlatform(platform: Platform): void {
    const model = toFormModel(platform);

    this.errorMessage.set(null);
    this.selected.set(platform);
    this.editorMode.set('edit');
    this.loadedTemplateType = null;
    this.model.set(model);
    this.createPlatformBaseline.set(null);
    this.updatePlatformBaseline.set(toUpdatePlatformRequest(model));
  }

  private openNewPlatform(): void {
    const model = createPlatformFormModel();

    this.errorMessage.set(null);
    this.selected.set(null);
    this.editorMode.set('create');
    this.loadedTemplateType = null;
    this.model.set(model);
    this.createPlatformBaseline.set(toCreatePlatformRequest(model));
    this.updatePlatformBaseline.set(null);
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    this.save();
  }

  protected save(): void {
    if (this.saveDisabled()) {
      return;
    }

    if (this.form().invalid()) {
      this.form().markAsTouched();
      return;
    }

    if (this.editorMode() === 'create') {
      this.createPlatform();
    } else {
      this.updatePlatform();
    }
  }

  protected deleteSelected(): void {
    if (this.isSaving() || this.isDeleting()) {
      return;
    }

    this.discardPlatformChangesBefore(() => this.deleteSelectedAfterDiscard());
  }

  private deleteSelectedAfterDiscard(): void {
    const current = this.selected();
    if (current === null || this.isSaving() || this.isDeleting()) {
      return;
    }

    this.errorMessage.set(null);
    this.isDeleting.set(true);

    this.platformsService
      .delete(current.type, current.id)
      .pipe(finalize(() => this.isDeleting.set(false)))
      .subscribe({
        next: () => {
          this.removeFromList(current.id);
          this.notifications.showSuccess('Platform deleted.');
        },
        error: () => {
          this.errorMessage.set('The platform could not be deleted. Try again.');
        },
      });
  }

  private confirmDiscardPlatformChanges(): Observable<boolean> {
    return this.confirmation
      .confirm<'keep-editing' | 'discard'>({
        kind: 'warning',
        title: 'Discard unsaved platform changes?',
        body: 'Platform edits have not been saved.',
        actions: [
          { id: 'keep-editing', label: 'Keep editing' },
          { id: 'discard', label: 'Discard changes', primary: true },
        ],
      })
      .pipe(map((result) => result === 'discard'));
  }

  private discardPlatformChangesBefore(action: () => void): void {
    if (!this.hasPendingPlatformChanges()) {
      action();
      return;
    }

    this.confirmDiscardPlatformChanges().subscribe((discard) => {
      if (discard) {
        action();
      }
    });
  }

  private loadPlatforms(): void {
    this.isLoading.set(true);
    this.loadFailed.set(false);

    this.platformsService
      .list()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (response) => {
          this.platforms.set(sortPlatforms(response.platforms));
        },
        error: () => {
          this.platforms.set([]);
          this.loadFailed.set(true);
        },
      });
  }

  private createPlatform(): void {
    const request = toCreatePlatformRequest(this.model());

    this.errorMessage.set(null);
    this.isSaving.set(true);

    this.platformsService
      .create(request)
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (response) => {
          const created: Platform = {
            id: response.id,
            name: response.name,
            referenceKey: response.referenceKey,
            type: response.type,
            publishSettings: response.publishSettings,
            publishingContent: response.publishingContent,
          };
          this.platforms.update((list) => sortPlatforms([created, ...list]));
          this.selected.set(created);
          this.editorMode.set('edit');
          this.model.set(toFormModel(created));
          this.createPlatformBaseline.set(null);
          this.updatePlatformBaseline.set(toUpdatePlatformRequest(this.model()));
          this.notifications.showSuccess('Platform created.');
        },
        error: (error: unknown) => {
          this.errorMessage.set(describeSaveError(error));
        },
      });
  }

  private updatePlatform(): void {
    const current = this.selected();
    if (current === null) {
      return;
    }

    const request = toUpdatePlatformRequest(this.model());

    this.errorMessage.set(null);
    this.isSaving.set(true);

    // The type is immutable, so the original type and id locate the row.
    this.platformsService
      .update(current.type, current.id, request)
      .pipe(finalize(() => this.isSaving.set(false)))
      .subscribe({
        next: (response) => {
          const updated: Platform = {
            id: response.id,
            type: response.type,
            name: response.name,
            referenceKey: response.referenceKey,
            publishSettings: response.publishSettings,
            publishingContent: response.publishingContent,
          };
          this.platforms.update((list) =>
            sortPlatforms(list.map((entry) => (entry.id === current.id ? updated : entry))),
          );
          this.selected.set(updated);
          this.model.set(toFormModel(updated));
          this.createPlatformBaseline.set(null);
          this.updatePlatformBaseline.set(toUpdatePlatformRequest(this.model()));
          this.notifications.showSuccess('Platform saved.');
        },
        error: (error: unknown) => {
          this.errorMessage.set(describeSaveError(error));
        },
      });
  }

  private removeFromList(id: string): void {
    this.platforms.update((list) => list.filter((entry) => entry.id !== id));
    this.closeEditor();
  }

  private closeEditor(): void {
    this.selected.set(null);
    this.editorMode.set('none');
    this.model.set(createPlatformFormModel());
    this.createPlatformBaseline.set(null);
    this.updatePlatformBaseline.set(null);
    this.errorMessage.set(null);
  }

  private loadTemplates(type: Platform['type']): void {
    const loadId = ++this.latestTemplateLoadId;
    this.templateLoadFailed.set(false);

    this.templatesService.list(type).subscribe({
      next: (response) => {
        if (loadId !== this.latestTemplateLoadId) {
          return;
        }

        this.availableTemplates.set(sortTemplates(response.templates));
        this.resetUnavailableTemplateIds(response.templates);
      },
      error: () => {
        if (loadId !== this.latestTemplateLoadId) {
          return;
        }

        this.loadedTemplateType = null;
        this.availableTemplates.set([]);
        this.templateLoadFailed.set(true);
      },
    });
  }

  private resetUnavailableTemplateIds(templates: readonly Template[]): void {
    if (this.editorMode() !== 'create') {
      return;
    }

    const model = this.model();
    const availableIds = new Set(templates.map((template) => template.id));
    const titleTemplateId = availableIds.has(model.titleTemplateId)
      ? model.titleTemplateId
      : '';
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
}

// Maps a stored platform into the flat editor model. Missing YouTube settings
// fall back to the create defaults so the form always has bindable values.
function toFormModel(platform: Platform): PlatformFormModel {
  const defaults = createPlatformFormModel();
  const youTubeSettings = isYouTubeSettings(platform.publishSettings)
    ? platform.publishSettings
    : undefined;
  const wordPressSettings = isWordPressSettings(platform.publishSettings)
    ? platform.publishSettings
    : undefined;

  return {
    type: platform.type,
    name: platform.name,
    referenceKey: platform.referenceKey ?? defaults.referenceKey,
    titleTemplateId: platform.publishingContent.titleTemplateId,
    descriptionTemplateId: platform.publishingContent.descriptionTemplateId,
    youTubeClientId: youTubeSettings?.credentials.clientId ?? defaults.youTubeClientId,
    youTubeClientSecret: '',
    youTubeRefreshToken: '',
    youTubeClientSecretConfigured: String(
      youTubeSettings?.credentials.clientSecretConfigured ?? false,
    ),
    youTubeRefreshTokenConfigured: String(
      youTubeSettings?.credentials.refreshTokenConfigured ?? false,
    ),
    youTubeClientSecretDisplayValue:
      youTubeSettings?.credentials.clientSecretDisplayValue ??
      defaults.youTubeClientSecretDisplayValue,
    youTubeRefreshTokenDisplayValue:
      youTubeSettings?.credentials.refreshTokenDisplayValue ??
      defaults.youTubeRefreshTokenDisplayValue,
    youTubePrivacyStatus: youTubeSettings?.privacyStatus ?? defaults.youTubePrivacyStatus,
    youTubeMadeForKids: String(
      youTubeSettings?.selfDeclaredMadeForKids ?? defaults.youTubeMadeForKids,
    ),
    wordPressSiteUrl: wordPressSettings?.siteUrl ?? defaults.wordPressSiteUrl,
    wordPressUsername: wordPressSettings?.username ?? defaults.wordPressUsername,
    wordPressApplicationPassword: '',
    wordPressPostStatus: wordPressSettings?.postStatus ?? defaults.wordPressPostStatus,
    wordPressApplicationPasswordConfigured: String(
      wordPressSettings?.applicationPasswordConfigured ?? false,
    ),
    wordPressPasswordDisplayValue:
      wordPressSettings?.passwordDisplayValue ?? defaults.wordPressPasswordDisplayValue,
  };
}

function isYouTubeSettings(
  settings: Platform['publishSettings'],
): settings is YouTubePublishSettings {
  return settings !== undefined && 'credentials' in settings;
}

function isWordPressSettings(
  settings: Platform['publishSettings'],
): settings is WordPressPublishSettings {
  return settings !== undefined && 'siteUrl' in settings;
}

// The list order is not significant, so sort client-side by type then name for
// a stable, predictable display.
function sortPlatforms(platforms: readonly Platform[]): Platform[] {
  return [...platforms].sort((left, right) => {
    const byType = left.type.localeCompare(right.type);
    return byType !== 0 ? byType : left.name.localeCompare(right.name);
  });
}

function sortTemplates(templates: readonly Template[]): Template[] {
  return [...templates].sort((left, right) => left.name.localeCompare(right.name));
}

function isPlatformType(value: string): value is Platform['type'] {
  return value === 'YouTube' || value === 'WordPress';
}

function describeSaveError(error: unknown): string {
  if (error instanceof PlatformNameConflictError) {
    return 'A platform with this name already exists.';
  }

  if (error instanceof PlatformReferenceKeyConflictError) {
    return 'A platform with this reference key already exists.';
  }

  return 'The platform could not be saved. Try again.';
}
