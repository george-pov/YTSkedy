import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { describe, expect, it } from 'vitest';

import { Platform } from 'src/app/shared/api/platforms/platforms-service';
import {
  createPlatformFormModel,
  applyPlatformRules,
  PlatformFormModel,
  sameCreatePlatformRequest,
  sameUpdatePlatformRequest,
  toCreatePlatformRequest,
  toPlatformFormModel,
  toUpdatePlatformRequest,
} from './platforms.form';

describe('platforms form request mapping', () => {
  it('uses non-sticky Draft defaults with no scheduled offset', () => {
    expect(createPlatformFormModel()).toMatchObject({
      wordPressPostStatus: 'draft',
      wordPressCategoryIds: [],
      wordPressSticky: false,
      wordPressScheduleOffsetHours: '',
    });
  });

  it('maps selected title and description templates to request publishing content', () => {
    const model = validModel({
      titleTemplateId: ' title-template ',
      descriptionTemplateId: ' description-template ',
    });

    expect(toCreatePlatformRequest(model).publishingContent).toEqual({
      titleTemplateId: 'title-template',
      descriptionTemplateId: 'description-template',
    });
    expect(toUpdatePlatformRequest(model).publishingContent).toEqual({
      titleTemplateId: 'title-template',
      descriptionTemplateId: 'description-template',
    });
  });

  it('toUpdatePlatformRequest_YouTubeBlankSecrets_OmitsReplacementSecrets', () => {
    const request = toUpdatePlatformRequest(
      validModel({
        youTubeClientSecret: '   ',
        youTubeRefreshToken: '',
        youTubeClientSecretConfigured: 'true',
        youTubeRefreshTokenConfigured: 'true',
        youTubeClientSecretDisplayValue: '*********A3B',
        youTubeRefreshTokenDisplayValue: '*********Z9Y',
      }),
    );

    expect(request.publishSettings).toEqual({
      credentials: {
        clientId: 'client-id',
      },
      privacyStatus: 'private',
      selfDeclaredMadeForKids: false,
      categoryId: null,
      containsSyntheticMedia: false,
    });
  });

  it('toUpdatePlatformRequest_WordPressBlankApplicationPassword_OmitsReplacementSecret', () => {
    const request = toUpdatePlatformRequest(
      validModel({
        type: 'WordPress',
        youTubeClientId: '',
        youTubeClientSecret: '',
        youTubeRefreshToken: '',
        wordPressSiteUrl: ' https://blog.example.test/ ',
        wordPressUsername: ' publisher ',
        wordPressApplicationPassword: '   ',
        wordPressPostStatus: 'draft',
        wordPressApplicationPasswordConfigured: 'true',
        wordPressPasswordDisplayValue: '*******',
      }),
    );

    expect(request.publishSettings).toEqual({
      siteUrl: 'https://blog.example.test/',
      username: 'publisher',
      postStatus: 'draft',
      categoryIds: [],
      sticky: false,
    });
  });

  it('maps Scheduled settings to a numeric offset and sticky boolean', () => {
    const request = toCreatePlatformRequest(
      validModel({
        type: 'WordPress',
        wordPressSiteUrl: 'https://blog.example.test/',
        wordPressUsername: 'publisher',
        wordPressApplicationPassword: 'local-test-password',
        wordPressPostStatus: 'future',
        wordPressCategoryIds: [12, 34],
        wordPressSticky: true,
        wordPressScheduleOffsetHours: ' 24 ',
      }),
    );

    expect(request.publishSettings).toEqual({
      siteUrl: 'https://blog.example.test/',
      username: 'publisher',
      postStatus: 'future',
      categoryIds: [12, 34],
      sticky: true,
      scheduleOffsetHours: 24,
      applicationPassword: 'local-test-password',
    });
  });

  it('maps WordPress create defaults to an empty category ID array', () => {
    const request = toCreatePlatformRequest(
      validModel({
        type: 'WordPress',
        wordPressSiteUrl: 'https://blog.example.test/',
        wordPressUsername: 'publisher',
        wordPressApplicationPassword: 'local-test-password',
      }),
    );

    expect(request.publishSettings).toMatchObject({ categoryIds: [] });
  });

  it('maps selected category IDs to copied create and update requests', () => {
    const categoryIds = [34, 12];
    const model = validModel({
      type: 'WordPress',
      wordPressSiteUrl: 'https://blog.example.test/',
      wordPressUsername: 'publisher',
      wordPressApplicationPassword: 'local-test-password',
      wordPressCategoryIds: categoryIds,
    });

    const create = toCreatePlatformRequest(model);
    const update = toUpdatePlatformRequest(model);
    categoryIds[0] = 99;

    expect(create.publishSettings).toMatchObject({ categoryIds: [34, 12] });
    expect(update.publishSettings).toMatchObject({ categoryIds: [34, 12] });
  });

  it('omits a retained scheduled offset from non-scheduled requests', () => {
    const request = toUpdatePlatformRequest(
      validModel({
        type: 'WordPress',
        wordPressSiteUrl: 'https://blog.example.test/',
        wordPressUsername: 'publisher',
        wordPressPostStatus: 'pending',
        wordPressScheduleOffsetHours: '24',
      }),
    );

    expect(request.publishSettings).toEqual({
      siteUrl: 'https://blog.example.test/',
      username: 'publisher',
      postStatus: 'pending',
      categoryIds: [],
      sticky: false,
    });
  });

  it('toUpdatePlatformRequest_DisplayValues_DoesNotCopyDisplayValues', () => {
    const request = toUpdatePlatformRequest(
      validModel({
        youTubeClientSecret: '',
        youTubeRefreshToken: '',
        youTubeClientSecretConfigured: 'true',
        youTubeRefreshTokenConfigured: 'true',
        youTubeClientSecretDisplayValue: '*********A3B',
        youTubeRefreshTokenDisplayValue: '*********Z9Y',
        wordPressPasswordDisplayValue: '*******',
      }),
    );

    const json = JSON.stringify(request);

    expect(json).not.toContain('clientSecretDisplayValue');
    expect(json).not.toContain('refreshTokenDisplayValue');
    expect(json).not.toContain('passwordDisplayValue');
    expect(json).not.toContain('*********A3B');
    expect(json).not.toContain('*********Z9Y');
    expect(json).not.toContain('*******');
  });

  it('sameCreatePlatformRequest_NormalizedWhitespace_ComparesClean', () => {
    const saved = toCreatePlatformRequest(
      validModel({
        name: 'Main YouTube channel',
        referenceKey: 'youTube1',
        titleTemplateId: 'youtube-title-template',
        descriptionTemplateId: 'youtube-description-template',
      }),
    );
    const edited = toCreatePlatformRequest(
      validModel({
        name: '  Main YouTube channel  ',
        referenceKey: '  youTube1  ',
        titleTemplateId: '  youtube-title-template  ',
        descriptionTemplateId: '  youtube-description-template  ',
      }),
    );

    expect(sameCreatePlatformRequest(edited, saved)).toBe(true);
  });

  it('sameUpdatePlatformRequest_WordPressNormalizedWhitespace_ComparesClean', () => {
    const saved = toUpdatePlatformRequest(
      validModel({
        type: 'WordPress',
        name: 'Company blog',
        referenceKey: 'blog-1',
        wordPressSiteUrl: 'https://blog.example.test/',
        wordPressUsername: 'publisher',
        wordPressApplicationPassword: '',
        wordPressApplicationPasswordConfigured: 'true',
        titleTemplateId: 'wordpress-title-template',
        descriptionTemplateId: 'wordpress-description-template',
      }),
    );
    const edited = toUpdatePlatformRequest(
      validModel({
        type: 'WordPress',
        name: '  Company blog  ',
        referenceKey: '  blog-1  ',
        wordPressSiteUrl: '  https://blog.example.test/  ',
        wordPressUsername: '  publisher  ',
        wordPressApplicationPassword: '',
        wordPressApplicationPasswordConfigured: 'true',
        titleTemplateId: '  wordpress-title-template  ',
        descriptionTemplateId: '  wordpress-description-template  ',
      }),
    );

    expect(sameUpdatePlatformRequest(edited, saved)).toBe(true);
  });

  it('sameUpdatePlatformRequest_BlankYouTubeSecretsPreserveStoredSecrets', () => {
    const saved = toUpdatePlatformRequest(
      validModel({
        youTubeClientSecret: '',
        youTubeRefreshToken: '',
        youTubeClientSecretConfigured: 'true',
        youTubeRefreshTokenConfigured: 'true',
        youTubeClientSecretDisplayValue: '*********A3B',
        youTubeRefreshTokenDisplayValue: '*********Z9Y',
      }),
    );
    const edited = toUpdatePlatformRequest(
      validModel({
        youTubeClientSecret: '   ',
        youTubeRefreshToken: '   ',
        youTubeClientSecretConfigured: 'true',
        youTubeRefreshTokenConfigured: 'true',
        youTubeClientSecretDisplayValue: '*********NEW',
        youTubeRefreshTokenDisplayValue: '*********DIFFERENT',
      }),
    );

    expect(sameUpdatePlatformRequest(edited, saved)).toBe(true);
  });

  it('sameUpdatePlatformRequest_BlankWordPressApplicationPasswordPreservesStoredSecret', () => {
    const saved = toUpdatePlatformRequest(
      validModel({
        type: 'WordPress',
        wordPressSiteUrl: 'https://blog.example.test/',
        wordPressUsername: 'publisher',
        wordPressApplicationPassword: '',
        wordPressApplicationPasswordConfigured: 'true',
        wordPressPasswordDisplayValue: '*******',
      }),
    );
    const edited = toUpdatePlatformRequest(
      validModel({
        type: 'WordPress',
        wordPressSiteUrl: 'https://blog.example.test/',
        wordPressUsername: 'publisher',
        wordPressApplicationPassword: '   ',
        wordPressApplicationPasswordConfigured: 'true',
        wordPressPasswordDisplayValue: 'changed display text',
      }),
    );

    expect(sameUpdatePlatformRequest(edited, saved)).toBe(true);
  });

  it('sameUpdatePlatformRequest_WordPressStickyChangeComparesDirty', () => {
    const saved = toUpdatePlatformRequest(
      validModel({ type: 'WordPress', wordPressSticky: false }),
    );
    const edited = toUpdatePlatformRequest(
      validModel({ type: 'WordPress', wordPressSticky: true }),
    );

    expect(sameUpdatePlatformRequest(edited, saved)).toBe(false);
  });

  it('sameUpdatePlatformRequest_CategoryOrderChangeComparesDirty', () => {
    const saved = toUpdatePlatformRequest(
      validModel({ type: 'WordPress', wordPressCategoryIds: [12, 34] }),
    );
    const edited = toUpdatePlatformRequest(
      validModel({ type: 'WordPress', wordPressCategoryIds: [34, 12] }),
    );

    expect(sameUpdatePlatformRequest(edited, saved)).toBe(false);
  });

  it('sameUpdatePlatformRequest_CategoryAddOrRemoveComparesDirty', () => {
    const saved = toUpdatePlatformRequest(
      validModel({ type: 'WordPress', wordPressCategoryIds: [12] }),
    );
    const added = toUpdatePlatformRequest(
      validModel({ type: 'WordPress', wordPressCategoryIds: [12, 34] }),
    );
    const removed = toUpdatePlatformRequest(
      validModel({ type: 'WordPress', wordPressCategoryIds: [] }),
    );

    expect(sameUpdatePlatformRequest(added, saved)).toBe(false);
    expect(sameUpdatePlatformRequest(removed, saved)).toBe(false);
  });

  it('sameUpdatePlatformRequest_ScheduledOffsetChangeComparesDirty', () => {
    const saved = toUpdatePlatformRequest(
      validModel({
        type: 'WordPress',
        wordPressPostStatus: 'future',
        wordPressScheduleOffsetHours: '24',
      }),
    );
    const edited = toUpdatePlatformRequest(
      validModel({
        type: 'WordPress',
        wordPressPostStatus: 'future',
        wordPressScheduleOffsetHours: '48',
      }),
    );

    expect(sameUpdatePlatformRequest(edited, saved)).toBe(false);
  });

  it('sameUpdatePlatformRequest_RetainedHiddenOffsetComparesClean', () => {
    const saved = toUpdatePlatformRequest(
      validModel({
        type: 'WordPress',
        wordPressPostStatus: 'draft',
        wordPressScheduleOffsetHours: '',
      }),
    );
    const edited = toUpdatePlatformRequest(
      validModel({
        type: 'WordPress',
        wordPressPostStatus: 'draft',
        wordPressScheduleOffsetHours: '24',
      }),
    );

    expect(sameUpdatePlatformRequest(edited, saved)).toBe(true);
  });

  it('sameUpdatePlatformRequest_ReplacementSecretValuesCompareDirty', () => {
    const saved = toUpdatePlatformRequest(
      validModel({
        youTubeClientSecret: '',
        youTubeRefreshToken: '',
        youTubeClientSecretConfigured: 'true',
        youTubeRefreshTokenConfigured: 'true',
      }),
    );
    const edited = toUpdatePlatformRequest(
      validModel({
        youTubeClientSecret: 'replacement-client-secret',
        youTubeRefreshToken: '',
        youTubeClientSecretConfigured: 'true',
        youTubeRefreshTokenConfigured: 'true',
      }),
    );

    expect(sameUpdatePlatformRequest(edited, saved)).toBe(false);
  });

  it('sameUpdatePlatformRequest_TitleOrDescriptionTemplateChangesCompareDirty', () => {
    const saved = toUpdatePlatformRequest(
      validModel({
        titleTemplateId: 'title-template',
        descriptionTemplateId: 'description-template',
      }),
    );

    expect(
      sameUpdatePlatformRequest(
        toUpdatePlatformRequest(validModel({ titleTemplateId: 'changed-title-template' })),
        saved,
      ),
    ).toBe(false);
    expect(
      sameUpdatePlatformRequest(
        toUpdatePlatformRequest(
          validModel({ descriptionTemplateId: 'changed-description-template' }),
        ),
        saved,
      ),
    ).toBe(false);
  });

  it('maps YouTube response settings to the form without copying replacement secrets', () => {
    const model = toPlatformFormModel(
      platform({
        publishSettings: {
          credentials: {
            clientId: 'client-id',
            clientSecretConfigured: true,
            refreshTokenConfigured: true,
            clientSecretDisplayValue: '*********A3B',
            refreshTokenDisplayValue: '*********Z9Y',
          },
          privacyStatus: 'unlisted',
          selfDeclaredMadeForKids: true,
          categoryId: '27',
          containsSyntheticMedia: true,
        },
      }),
    );

    expect(model).toMatchObject({
      type: 'YouTube',
      name: 'Main YouTube channel',
      referenceKey: 'youTube1',
      titleTemplateId: 'title-template',
      descriptionTemplateId: 'description-template',
      youTubeClientId: 'client-id',
      youTubeClientSecret: '',
      youTubeRefreshToken: '',
      youTubeClientSecretConfigured: 'true',
      youTubeRefreshTokenConfigured: 'true',
      youTubeClientSecretDisplayValue: '*********A3B',
      youTubeRefreshTokenDisplayValue: '*********Z9Y',
      youTubePrivacyStatus: 'unlisted',
      youTubeMadeForKids: 'true',
      youTubeCategoryId: '27',
      youTubeContainsSyntheticMedia: 'true',
    });
  });

  it('maps legacy YouTube response settings to default category and disclosure', () => {
    const model = toPlatformFormModel(platform());

    expect(model.youTubeCategoryId).toBe('');
    expect(model.youTubeContainsSyntheticMedia).toBe('false');
  });

  it('maps YouTube category and disclosure into requests and dirty comparison', () => {
    const baseline = toUpdatePlatformRequest(validModel({}));
    const changed = toUpdatePlatformRequest(
      validModel({
        youTubeCategoryId: ' 27 ',
        youTubeContainsSyntheticMedia: 'true',
      }),
    );

    expect(changed.publishSettings).toMatchObject({
      categoryId: '27',
      containsSyntheticMedia: true,
    });
    expect(sameUpdatePlatformRequest(baseline, changed)).toBe(false);
  });

  it('maps WordPress response settings to the form without copying replacement secrets', () => {
    const model = toPlatformFormModel(
      platform({
        type: 'WordPress',
        referenceKey: 'blog-1',
        publishSettings: {
          siteUrl: 'https://blog.example.test/',
          username: 'publisher',
          postStatus: 'publish',
          categoryIds: [34, 12],
          applicationPasswordConfigured: true,
          passwordDisplayValue: '*******',
        },
      }),
    );

    expect(model).toMatchObject({
      type: 'WordPress',
      referenceKey: 'blog-1',
      wordPressSiteUrl: 'https://blog.example.test/',
      wordPressUsername: 'publisher',
      wordPressApplicationPassword: '',
      wordPressPostStatus: 'publish',
      wordPressCategoryIds: [34, 12],
      wordPressSticky: false,
      wordPressScheduleOffsetHours: '',
      wordPressApplicationPasswordConfigured: 'true',
      wordPressPasswordDisplayValue: '*******',
    });
  });

  it('copies response category IDs and restores them from the saved platform baseline', () => {
    const categoryIds = [34, 12];
    const saved = platform({
      type: 'WordPress',
      publishSettings: {
        siteUrl: 'https://blog.example.test/',
        username: 'publisher',
        postStatus: 'draft',
        categoryIds,
      },
    });

    const editedModel = toPlatformFormModel(saved);
    editedModel.wordPressCategoryIds.splice(0, 1);
    const restoredModel = toPlatformFormModel(saved);
    categoryIds[0] = 99;

    expect(restoredModel.wordPressCategoryIds).toEqual([34, 12]);
  });

  it('restores Scheduled response settings into the form', () => {
    const model = toPlatformFormModel(
      platform({
        type: 'WordPress',
        publishSettings: {
          siteUrl: 'https://blog.example.test/',
          username: 'publisher',
          postStatus: 'future',
          categoryIds: [12, 34],
          sticky: true,
          scheduleOffsetHours: 24,
          applicationPasswordConfigured: true,
          passwordDisplayValue: '*******',
        },
      }),
    );

    expect(model).toMatchObject({
      wordPressPostStatus: 'future',
      wordPressCategoryIds: [12, 34],
      wordPressSticky: true,
      wordPressScheduleOffsetHours: '24',
      wordPressApplicationPassword: '',
      wordPressPasswordDisplayValue: '*******',
    });
  });

  it('falls back to create defaults when response settings are missing', () => {
    const youTubeModel = toPlatformFormModel(platform({ publishSettings: undefined }));
    const wordPressModel = toPlatformFormModel(
      platform({ type: 'WordPress', publishSettings: undefined }),
    );

    expect(youTubeModel).toMatchObject({
      youTubeClientId: '',
      youTubeClientSecret: '',
      youTubeRefreshToken: '',
      youTubeClientSecretConfigured: 'false',
      youTubeRefreshTokenConfigured: 'false',
      youTubePrivacyStatus: 'private',
      youTubeMadeForKids: 'false',
    });
    expect(wordPressModel).toMatchObject({
      wordPressSiteUrl: '',
      wordPressUsername: '',
      wordPressApplicationPassword: '',
      wordPressPostStatus: 'draft',
      wordPressCategoryIds: [],
      wordPressSticky: false,
      wordPressScheduleOffsetHours: '',
      wordPressApplicationPasswordConfigured: 'false',
      wordPressPasswordDisplayValue: '',
    });
  });

  it('requires an offset for Scheduled posts', () => {
    const offset = scheduledOffsetField('');

    expect(offset().errors()[0]?.message).toBe(
      'Hours before event start is required for Scheduled posts.',
    );
  });

  it.each(['0', '169', '-1', '+1', '1.5', '1e2', '24 hours'])(
    'rejects invalid Scheduled offset %s',
    (value) => {
      const offset = scheduledOffsetField(value);

      expect(offset().errors()[0]?.message).toBe(
        'Hours before event start must be a whole number from 1 through 168.',
      );
    },
  );

  it.each(['1', '24', '168'])('accepts valid Scheduled offset %s', (value) => {
    expect(scheduledOffsetField(value)().errors()).toHaveLength(0);
  });

  it.each(['draft', 'pending', 'private', 'publish'])(
    'does not validate a hidden offset for %s',
    (postStatus) => {
      const model = signal(
        validModel({
          type: 'WordPress',
          wordPressPostStatus: postStatus,
          wordPressScheduleOffsetHours: 'invalid retained value',
        }),
      );
      const platformForm = TestBed.runInInjectionContext(() => form(model, applyPlatformRules));

      expect(platformForm.wordPressScheduleOffsetHours().errors()).toHaveLength(0);
    },
  );

  function scheduledOffsetField(value: string) {
    const model = signal(
      validModel({
        type: 'WordPress',
        wordPressSiteUrl: 'https://blog.example.test/',
        wordPressUsername: 'publisher',
        wordPressApplicationPassword: 'local-test-password',
        wordPressPostStatus: 'future',
        wordPressScheduleOffsetHours: value,
      }),
    );

    return TestBed.runInInjectionContext(() => form(model, applyPlatformRules))
      .wordPressScheduleOffsetHours;
  }

  function validModel(overrides: Partial<PlatformFormModel>): PlatformFormModel {
    return {
      ...createPlatformFormModel(),
      name: 'Main YouTube channel',
      youTubeClientId: 'client-id',
      youTubeClientSecret: 'client-secret',
      youTubeRefreshToken: 'refresh-token',
      ...overrides,
    };
  }

  function platform(overrides: Partial<Platform> = {}): Platform {
    return {
      id: 'id-1',
      name: 'Main YouTube channel',
      referenceKey: 'youTube1',
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
});
