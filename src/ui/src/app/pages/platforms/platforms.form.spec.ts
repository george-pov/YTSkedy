import { describe, expect, it } from 'vitest';

import {
  createPlatformFormModel,
  PlatformFormModel,
  toCreatePlatformRequest,
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
});
