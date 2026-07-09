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

    state.model.set({
      ...state.model(),
      name: 'Second channel',
    });

    expect(state.hasPendingPlatformChanges()).toBe(true);
    expect(state.saveDisabled()).toBe(false);
  });

  it('applies saved platforms as clean edit baselines', () => {
    const state = createState();

    state.openNewPlatform();
    state.applyCreatedPlatform(platform({ id: 'new-id', name: 'Second channel' }));

    expect(state.editorMode()).toBe('edit');
    expect(state.selected()?.id).toBe('new-id');
    expect(state.hasPendingPlatformChanges()).toBe(false);
  });

  it('uses mutation state to disable save', () => {
    const state = createState();

    state.openNewPlatform();
    state.model.set({
      ...state.model(),
      name: 'Second channel',
    });

    state.setSaving(true);

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

  function createState(): PlatformsEditorState {
    return TestBed.runInInjectionContext(() => new PlatformsEditorState());
  }
});
