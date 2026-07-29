import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { Platform } from 'src/app/shared/api/platforms/platforms-service';
import { PlatformsEditorState } from './platforms-editor.state';

describe('PlatformsEditorState', () => {
  it('sorts loaded platforms and opens the first row when the editor is closed', () => {
    const state = createState();

    state.applyLoadedPlatforms([
      platform({ id: 'yt-1', name: 'Main YouTube channel', type: 'YouTube' }),
      platform({ id: 'wp-1', name: 'Company blog', type: 'WordPress' }),
    ]);

    expect(state.platforms().map((entry) => entry.name)).toEqual([
      'Company blog',
      'Main YouTube channel',
    ]);
    expect(state.selected()?.id).toBe('wp-1');
    expect(state.editorMode()).toBe('edit');
  });

  it('opens a clean create editor with save disabled until normalized request changes', () => {
    const state = createState();

    state.openNewPlatform();

    expect(state.editorMode()).toBe('create');
    expect(state.saveDisabled()).toBe(true);
    expect(state.cancelDisabled()).toBe(true);

    state.model.set({
      ...state.model(),
      name: 'Second channel',
    });

    expect(state.hasPendingPlatformChanges()).toBe(true);
    expect(state.saveDisabled()).toBe(false);
    expect(state.cancelDisabled()).toBe(false);
  });

  it('restores a dirty create editor to the default provider and clears interaction state', () => {
    const state = createState();
    state.applyLoadedPlatforms([platform()]);
    state.openNewPlatform();
    state.model.set({
      ...state.model(),
      type: 'WordPress',
      name: 'Draft blog',
      referenceKey: 'draft-blog',
      titleTemplateId: 'wordpress-title',
      descriptionTemplateId: 'wordpress-description',
      wordPressSiteUrl: 'https://draft.example.test/',
      wordPressUsername: 'draft-publisher',
      wordPressApplicationPassword: 'replacement-password',
      wordPressCategoryIds: [34, 12],
      wordPressPostStatus: 'future',
      wordPressSticky: true,
      wordPressScheduleOffsetHours: '12',
    });
    state.form().markAsTouched();
    state.form().markAsDirty();
    state.setErrorMessage('The platform could not be saved. Try again.');

    state.restoreEditorBaseline();

    expect(state.editorMode()).toBe('create');
    expect(state.selected()).toBeNull();
    expect(state.model()).toMatchObject({
      type: 'YouTube',
      name: '',
      referenceKey: '',
      titleTemplateId: '',
      descriptionTemplateId: '',
      youTubePrivacyStatus: 'private',
      youTubeMadeForKids: 'false',
      youTubeCategoryId: '',
      youTubeContainsSyntheticMedia: 'false',
      youTubeDefaultAudioLanguage: '',
      youTubeDefaultLanguage: '',
    });
    expect(state.platforms().map((entry) => entry.id)).toEqual(['id-1']);
    expect(state.form().touched()).toBe(false);
    expect(state.form().dirty()).toBe(false);
    expect(state.errorMessage()).toBeNull();
    expect(state.hasPendingPlatformChanges()).toBe(false);
    expect(state.cancelDisabled()).toBe(true);
    expect(state.saveDisabled()).toBe(true);
  });

  it('applies saved platforms as clean edit baselines', () => {
    const state = createState();

    state.openNewPlatform();
    state.applyCreatedPlatform(platform({ id: 'new-id', name: 'Second channel' }));

    expect(state.editorMode()).toBe('edit');
    expect(state.selected()?.id).toBe('new-id');
    expect(state.hasPendingPlatformChanges()).toBe(false);
    expect(state.cancelDisabled()).toBe(true);
  });

  it('uses mutation state to disable save and Cancel', () => {
    const state = createState();

    state.openNewPlatform();
    state.model.set({
      ...state.model(),
      name: 'Second channel',
    });

    state.setSaving(true);

    expect(state.saveDisabled()).toBe(true);
    expect(state.cancelDisabled()).toBe(true);

    state.setSaving(false);
    state.setDeleting(true);

    expect(state.saveDisabled()).toBe(true);
    expect(state.cancelDisabled()).toBe(true);
  });

  it('restores every YouTube edit field without replacing redacted secrets or selection', () => {
    const state = createState();
    const selected = youTubePlatform();
    state.applyLoadedPlatforms([selected]);
    state.model.set({
      ...state.model(),
      name: 'Changed channel',
      referenceKey: 'changed-key',
      titleTemplateId: 'changed-title',
      descriptionTemplateId: 'changed-description',
      youTubeClientId: 'changed-client-id',
      youTubeClientSecret: 'replacement-client-secret',
      youTubeRefreshToken: 'replacement-refresh-token',
      youTubePrivacyStatus: 'private',
      youTubeMadeForKids: 'false',
      youTubeCategoryId: '24',
      youTubeContainsSyntheticMedia: 'false',
      youTubeDefaultAudioLanguage: 'fr',
      youTubeDefaultLanguage: 'de',
    });
    state.form().markAsTouched();
    state.form().markAsDirty();
    state.setErrorMessage('The platform could not be saved. Try again.');

    expect(state.cancelDisabled()).toBe(false);

    state.restoreEditorBaseline();

    expect(state.editorMode()).toBe('edit');
    expect(state.selected()).toBe(selected);
    expect(state.selected()?.id).toBe('youtube-id');
    expect(state.model()).toMatchObject({
      type: 'YouTube',
      name: 'Main YouTube channel',
      referenceKey: 'youtube-main',
      titleTemplateId: 'youtube-title',
      descriptionTemplateId: 'youtube-description',
      youTubeClientId: 'stored-client-id',
      youTubeClientSecret: '',
      youTubeRefreshToken: '',
      youTubeClientSecretConfigured: 'true',
      youTubeRefreshTokenConfigured: 'true',
      youTubeClientSecretDisplayValue: '*********A3B',
      youTubeRefreshTokenDisplayValue: '*********Z9Y',
      youTubePrivacyStatus: 'unlisted',
      youTubeMadeForKids: 'true',
      youTubeCategoryId: '20',
      youTubeContainsSyntheticMedia: 'true',
      youTubeDefaultAudioLanguage: 'en-US',
      youTubeDefaultLanguage: 'ru',
    });
    expect(state.form().touched()).toBe(false);
    expect(state.form().dirty()).toBe(false);
    expect(state.errorMessage()).toBeNull();
    expect(state.hasPendingPlatformChanges()).toBe(false);
    expect(state.cancelDisabled()).toBe(true);
    expect(state.saveDisabled()).toBe(true);
  });

  it('restores every WordPress edit field and preserves ordered categories and selection', () => {
    const state = createState();
    const selected = wordPressPlatform();
    state.applyLoadedPlatforms([selected]);
    state.model.set({
      ...state.model(),
      name: 'Changed blog',
      referenceKey: 'changed-key',
      titleTemplateId: 'changed-title',
      descriptionTemplateId: 'changed-description',
      wordPressSiteUrl: 'https://changed.example.test/',
      wordPressUsername: 'changed-publisher',
      wordPressApplicationPassword: 'replacement-password',
      wordPressCategoryIds: [99],
      wordPressPostStatus: 'publish',
      wordPressSticky: false,
      wordPressScheduleOffsetHours: '48',
    });
    state.form().markAsTouched();
    state.form().markAsDirty();
    state.setErrorMessage('The platform could not be saved. Try again.');

    expect(state.cancelDisabled()).toBe(false);

    state.restoreEditorBaseline();

    expect(state.editorMode()).toBe('edit');
    expect(state.selected()).toBe(selected);
    expect(state.selected()?.id).toBe('wordpress-id');
    expect(state.model()).toMatchObject({
      type: 'WordPress',
      name: 'Company blog',
      referenceKey: 'company-blog',
      titleTemplateId: 'wordpress-title',
      descriptionTemplateId: 'wordpress-description',
      wordPressSiteUrl: 'https://blog.example.test/',
      wordPressUsername: 'publisher',
      wordPressApplicationPassword: '',
      wordPressApplicationPasswordConfigured: 'true',
      wordPressPasswordDisplayValue: '*******K7M',
      wordPressCategoryIds: [34, 12],
      wordPressPostStatus: 'future',
      wordPressSticky: true,
      wordPressScheduleOffsetHours: '24',
    });
    expect(state.form().touched()).toBe(false);
    expect(state.form().dirty()).toBe(false);
    expect(state.errorMessage()).toBeNull();
    expect(state.hasPendingPlatformChanges()).toBe(false);
    expect(state.cancelDisabled()).toBe(true);
    expect(state.saveDisabled()).toBe(true);
  });

  function platform(overrides: Partial<Platform> = {}): Platform {
    return {
      id: 'id-1',
      name: 'Main YouTube channel',
      referenceKey: null,
      type: 'YouTube',
      publishingContent: {
        titleTemplateId: 'title-template',
        descriptionTemplateId: 'description-template',
      },
      publishSettings: {
        credentials: {
          clientId: 'client-id',
          clientSecretConfigured: true,
          refreshTokenConfigured: true,
        },
        privacyStatus: 'private',
        selfDeclaredMadeForKids: false,
      },
      ...overrides,
    };
  }

  function youTubePlatform(): Platform {
    return platform({
      id: 'youtube-id',
      name: 'Main YouTube channel',
      referenceKey: 'youtube-main',
      publishingContent: {
        titleTemplateId: 'youtube-title',
        descriptionTemplateId: 'youtube-description',
      },
      publishSettings: {
        credentials: {
          clientId: 'stored-client-id',
          clientSecretConfigured: true,
          refreshTokenConfigured: true,
          clientSecretDisplayValue: '*********A3B',
          refreshTokenDisplayValue: '*********Z9Y',
        },
        privacyStatus: 'unlisted',
        selfDeclaredMadeForKids: true,
        categoryId: '20',
        containsSyntheticMedia: true,
        defaultAudioLanguage: 'en-US',
        defaultLanguage: 'ru',
      },
    });
  }

  function wordPressPlatform(): Platform {
    return platform({
      id: 'wordpress-id',
      name: 'Company blog',
      referenceKey: 'company-blog',
      type: 'WordPress',
      publishingContent: {
        titleTemplateId: 'wordpress-title',
        descriptionTemplateId: 'wordpress-description',
      },
      publishSettings: {
        siteUrl: 'https://blog.example.test/',
        username: 'publisher',
        applicationPasswordConfigured: true,
        passwordDisplayValue: '*******K7M',
        categoryIds: [34, 12],
        postStatus: 'future',
        sticky: true,
        scheduleOffsetHours: 24,
      },
    });
  }

  function createState(): PlatformsEditorState {
    return TestBed.runInInjectionContext(() => new PlatformsEditorState());
  }
});
