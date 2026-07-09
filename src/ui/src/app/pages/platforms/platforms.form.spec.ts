import { describe, expect, it } from 'vitest';

import { Platform } from 'src/app/shared/api/platforms/platforms-service';
import {
  createPlatformFormModel,
  PlatformFormModel,
  sameCreatePlatformRequest,
  sameUpdatePlatformRequest,
  toCreatePlatformRequest,
  toPlatformFormModel,
  toUpdatePlatformRequest,
} from './platforms.form';

describe('platforms form request mapping', () => {
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
    });
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
      wordPressApplicationPasswordConfigured: 'true',
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
      wordPressApplicationPasswordConfigured: 'false',
      wordPressPasswordDisplayValue: '',
    });
  });

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
