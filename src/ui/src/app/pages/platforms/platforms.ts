import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  type OnInit,
  signal,
} from '@angular/core';
import { form } from '@angular/forms/signals';
import { finalize } from 'rxjs';

import {
  Platform,
  PlatformNameConflictError,
  PlatformsService,
  YouTubePublishSettings,
  WordPressPublishSettings,
} from 'src/app/shared/api/platforms/platforms-service';
import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { DataTable } from 'src/app/shared/components/data-table/data-table';
import { DataTableColumn } from 'src/app/shared/components/data-table/data-table-column';
import { Input } from 'src/app/shared/components/input/input';
import { ProgressBar } from 'src/app/shared/components/progress-bar/progress-bar';
import { DelayedLoading } from 'src/app/shared/components/progress-bar/delayed-loading';
import { Select, SelectOption } from 'src/app/shared/components/select/select';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import {
  applyPlatformRules,
  createPlatformFormModel,
  PlatformFormModel,
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
    DelayedLoading,
  ],
  templateUrl: './platforms.html',
  styleUrl: './platforms.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Platforms implements OnInit {
  private readonly platformsService = inject(PlatformsService);
  private readonly notifications = inject(NotificationService);

  protected readonly platforms = signal<Platform[]>([]);
  protected readonly selected = signal<Platform | null>(null);
  protected readonly editorMode = signal<EditorMode>('none');

  protected readonly isLoading = signal(true);
  protected readonly loadFailed = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isDeleting = signal(false);
  // Single editor-scoped error surface for a failed save or delete.
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly columns: readonly DataTableColumn<Platform>[] = [
    { key: 'type', header: 'Type', value: (platform) => platform.type },
    { key: 'name', header: 'Name', value: (platform) => platform.name },
  ];

  protected readonly typeOptions: readonly SelectOption[] = [
    { value: 'YouTube', label: 'YouTube' },
    { value: 'WordPress', label: 'WordPress' },
  ];

  protected readonly model = signal<PlatformFormModel>(createPlatformFormModel());
  protected readonly form = form(this.model, applyPlatformRules);

  // The type currently in the editor. Reactive to the create-mode type select,
  // so the settings section can switch on it.
  protected readonly selectedType = computed(() => this.model().type);

  ngOnInit(): void {
    this.loadPlatforms();
  }

  protected select(id: string): void {
    const platform = this.platforms().find((entry) => entry.id === id);
    if (platform === undefined) {
      return;
    }

    this.errorMessage.set(null);
    this.selected.set(platform);
    this.editorMode.set('edit');
    this.model.set(toFormModel(platform));
  }

  protected newPlatform(): void {
    this.errorMessage.set(null);
    this.selected.set(null);
    this.editorMode.set('create');
    this.model.set(createPlatformFormModel());
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
      this.createPlatform();
    } else {
      this.updatePlatform();
    }
  }

  protected deleteSelected(): void {
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
            type: response.type,
            publishSettings: response.publishSettings,
          };
          this.platforms.update((list) => sortPlatforms([created, ...list]));
          this.selected.set(created);
          this.editorMode.set('edit');
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
            publishSettings: response.publishSettings,
          };
          this.platforms.update((list) =>
            sortPlatforms(list.map((entry) => (entry.id === current.id ? updated : entry))),
          );
          this.selected.set(updated);
          this.notifications.showSuccess('Platform saved.');
        },
        error: (error: unknown) => {
          this.errorMessage.set(describeSaveError(error));
        },
      });
  }

  private removeFromList(id: string): void {
    this.platforms.update((list) => list.filter((entry) => entry.id !== id));
    this.selected.set(null);
    this.editorMode.set('none');
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
    youTubeCredentials: youTubeSettings?.credentials ?? defaults.youTubeCredentials,
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

function describeSaveError(error: unknown): string {
  if (error instanceof PlatformNameConflictError) {
    return 'A platform with this name already exists.';
  }

  return 'The platform could not be saved. Try again.';
}
