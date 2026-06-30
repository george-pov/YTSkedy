import { describe, expect, it } from 'vitest';

import {
  createPlatformFormModel,
  PlatformFormModel,
  toCreatePlatformRequest,
  toUpdatePlatformRequest,
} from './platforms.form';

describe('platforms form request mapping', () => {
  it.each([
    ['', '', null, null],
    ['title-template', '', 'title-template', null],
    ['', 'description-template', null, 'description-template'],
    [' title-template ', ' description-template ', 'title-template', 'description-template'],
  ])(
    'maps title template %s and description template %s to request publishing content',
    (titleTemplateId, descriptionTemplateId, expectedTitleTemplateId, expectedDescriptionTemplateId) => {
      const model = validModel({
        titleTemplateId,
        descriptionTemplateId,
      });

      expect(toCreatePlatformRequest(model).publishingContent).toEqual({
        titleTemplateId: expectedTitleTemplateId,
        descriptionTemplateId: expectedDescriptionTemplateId,
      });
      expect(toUpdatePlatformRequest(model).publishingContent).toEqual({
        titleTemplateId: expectedTitleTemplateId,
        descriptionTemplateId: expectedDescriptionTemplateId,
      });
    },
  );

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
