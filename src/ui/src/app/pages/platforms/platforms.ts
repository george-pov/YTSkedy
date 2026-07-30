import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  type OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, map, Observable } from 'rxjs';

import {
  Platform,
  PlatformNameConflictError,
  PlatformReferenceKeyConflictError,
  PlatformsService,
} from 'src/app/shared/api/platforms/platforms-service';
import { Template, TemplatesService } from 'src/app/shared/api/templates/templates-service';
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
import { isPlatformType, platformTypeOptions } from 'src/app/shared/platforms/platform-types';
import { type PendingChangesAware } from 'src/app/shared/routing/pending-changes-guard';
import { toCreatePlatformRequest, toUpdatePlatformRequest } from './platforms.form';
import { PlatformsEditorState } from './platforms-editor.state';
import { YouTubeSettings } from './youtube-settings/youtube-settings';
import { WordPressSettings } from './wordpress-settings/wordpress-settings';

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
  private readonly destroyRef = inject(DestroyRef);
  private latestTemplateLoadId = 0;
  private loadedTemplateType: Platform['type'] | null = null;
  protected readonly editor = new PlatformsEditorState();

  protected readonly platforms = this.editor.platforms;
  protected readonly selected = this.editor.selected;
  protected readonly editorMode = this.editor.editorMode;
  protected readonly isSaving = this.editor.isSaving;
  protected readonly isDeleting = this.editor.isDeleting;
  protected readonly errorMessage = this.editor.errorMessage;
  protected readonly model = this.editor.model;
  protected readonly form = this.editor.form;
  protected readonly selectedType = this.editor.selectedType;
  protected readonly hasPendingPlatformChanges = this.editor.hasPendingPlatformChanges;
  protected readonly saveDisabled = this.editor.saveDisabled;
  protected readonly cancelDisabled = this.editor.cancelDisabled;
  protected readonly availableTemplates = signal<Template[]>([]);
  protected readonly templateLoadFailed = signal(false);

  protected readonly isLoading = signal(true);
  protected readonly showLoading = delayedLoading(() => this.isLoading());
  protected readonly loadFailed = signal(false);

  protected readonly columns: readonly DataTableColumn<Platform>[] = [
    { key: 'type', header: 'Type', value: (platform) => platform.type },
    { key: 'name', header: 'Name', value: (platform) => platform.name },
    {
      key: 'referenceKey',
      header: 'Reference key',
      value: (platform) => platform.referenceKey ?? '',
    },
  ];

  protected readonly typeOptions = platformTypeOptions;

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
    if (this.editor.hasActiveMutation()) {
      return false;
    }

    if (!this.hasPendingPlatformChanges()) {
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
    if (this.cancelDisabled()) {
      return;
    }

    this.confirmDiscardPlatformChanges().subscribe((discard) => {
      if (discard) {
        this.editor.restoreEditorBaseline();
      }
    });
  }

  private openPlatform(platform: Platform): void {
    this.loadedTemplateType = null;
    this.editor.openPlatform(platform);
  }

  private openNewPlatform(): void {
    this.loadedTemplateType = null;
    this.editor.openNewPlatform();
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
    if (this.editor.hasActiveMutation()) {
      return;
    }

    const current = this.selected();
    if (current === null) {
      return;
    }

    this.confirmDeletePlatform(current)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed) => {
        if (confirmed) {
          this.deletePlatform(current);
        }
      });
  }

  private confirmDeletePlatform(platform: Platform): Observable<boolean> {
    const unsavedChangesWarning = this.hasPendingPlatformChanges()
      ? ' Unsaved platform type, name, templates, and provider settings will also be lost.'
      : '';

    return this.confirmation.confirmDeletion({
      title: 'Delete platform?',
      body: `This removes "${platform.name}" and its provider settings from YTSkedy. Existing provider publications are not removed and can no longer be deleted through YTSkedy after this action.${unsavedChangesWarning}`,
      deleteLabel: 'Delete platform',
    });
  }

  private deletePlatform(platform: Platform): void {
    if (this.editor.hasActiveMutation()) {
      return;
    }

    this.editor.setErrorMessage(null);
    this.editor.setDeleting(true);

    this.platformsService
      .delete(platform.type, platform.id)
      .pipe(
        finalize(() => this.editor.setDeleting(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.editor.removeDeletedPlatform(platform.id);
          this.notifications.showSuccess('Platform deleted.');
        },
        error: () => {
          this.editor.setErrorMessage('The platform could not be deleted. Try again.');
        },
      });
  }

  private confirmDiscardPlatformChanges(): Observable<boolean> {
    return this.confirmation
      .confirm<'keep-editing' | 'discard'>({
        kind: 'warning',
        title: 'Discard unsaved platform changes?',
        body: 'Unsaved platform type, name, templates, and provider settings will be lost and cannot be recovered.',
        actions: [
          { id: 'keep-editing', label: 'Keep editing' },
          {
            id: 'discard',
            label: 'Discard changes',
            primary: true,
            variant: 'danger-filled',
          },
        ],
      })
      .pipe(
        map((result) => result === 'discard'),
        takeUntilDestroyed(this.destroyRef),
      );
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
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.editor.applyLoadedPlatforms(response.platforms);
        },
        error: () => {
          this.editor.resetAfterLoadFailure();
          this.loadFailed.set(true);
        },
      });
  }

  private createPlatform(): void {
    const request = toCreatePlatformRequest(this.model());

    this.editor.setErrorMessage(null);
    this.editor.setSaving(true);

    this.platformsService
      .create(request)
      .pipe(
        finalize(() => this.editor.setSaving(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.editor.applyCreatedPlatform(response);
          this.notifications.showSuccess('Platform created.');
        },
        error: (error: unknown) => {
          this.editor.setErrorMessage(describeSaveError(error));
        },
      });
  }

  private updatePlatform(): void {
    const current = this.selected();
    if (current === null) {
      return;
    }

    const request = toUpdatePlatformRequest(this.model());

    this.editor.setErrorMessage(null);
    this.editor.setSaving(true);

    // The type is immutable, so the original type and id locate the row.
    this.platformsService
      .update(current.type, current.id, request)
      .pipe(
        finalize(() => this.editor.setSaving(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (response) => {
          this.editor.applyUpdatedPlatform(response);
          this.notifications.showSuccess('Platform saved.');
        },
        error: (error: unknown) => {
          this.editor.setErrorMessage(describeSaveError(error));
        },
      });
  }

  private loadTemplates(type: Platform['type']): void {
    const loadId = ++this.latestTemplateLoadId;
    this.templateLoadFailed.set(false);

    this.templatesService
      .list(type)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          if (loadId !== this.latestTemplateLoadId) {
            return;
          }

          this.availableTemplates.set(sortTemplates(response.templates));
          this.editor.clearUnavailableTemplateIds(response.templates);
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
}

function sortTemplates(templates: readonly Template[]): Template[] {
  return [...templates].sort((left, right) => left.name.localeCompare(right.name));
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
