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
