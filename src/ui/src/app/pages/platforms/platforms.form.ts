import {
  applyWhen,
  maxLength,
  required,
  validate,
  type SchemaPathTree,
} from '@angular/forms/signals';

import {
  CreatePlatformRequest,
  Platform,
  PlatformPublishSettings,
  PublishingContent,
  PlatformType,
  UpdatePlatformRequest,
} from 'src/app/shared/api/platforms/platforms-service';
import { sameRequest } from 'src/app/shared/forms/request-comparison';
import { defaultPlatformType } from 'src/app/shared/platforms/platform-types';
import {
  applyWordPressSettingsRules,
  toWordPressPublishSettings,
  withWordPressSettingsFormModel,
  wordpressSettingsFormDefaults,
} from './wordpress-settings/wordpress-settings.form';
import {
  applyYouTubeSettingsRules,
  toYouTubePublishSettings,
  withYouTubeSettingsFormModel,
  youtubeSettingsFormDefaults,
} from './youtube-settings/youtube-settings.form';

export const nameMaxLength = 50;
export const referenceKeyMaxLength = 15;
export const referenceKeyPattern = /^[A-Za-z0-9-]*$/;

/**
 * Editable fields of the platform editor. YouTube settings are flattened here
 * and only apply when {@link PlatformFormModel.type} is `YouTube`. Booleans are
 * carried as strings so they bind to the shared string-based form controls; the
 * request mapping converts them back.
 */
export interface PlatformFormModel {
  type: string;
  name: string;
  referenceKey: string;
  titleTemplateId: string;
  descriptionTemplateId: string;
  youTubeClientId: string;
  youTubeClientSecret: string;
  youTubeRefreshToken: string;
  youTubeClientSecretConfigured: string;
  youTubeRefreshTokenConfigured: string;
  youTubeClientSecretDisplayValue: string;
  youTubeRefreshTokenDisplayValue: string;
  youTubePrivacyStatus: string;
  youTubeMadeForKids: string;
  youTubeCategoryId: string;
  youTubeContainsSyntheticMedia: string;
  youTubeDefaultAudioLanguage: string;
  youTubeDefaultLanguage: string;
  wordPressSiteUrl: string;
  wordPressUsername: string;
  wordPressApplicationPassword: string;
  wordPressPostStatus: string;
  wordPressCategoryIds: number[];
  wordPressSticky: boolean;
  wordPressScheduleOffsetHours: string;
  wordPressApplicationPasswordConfigured: string;
  wordPressPasswordDisplayValue: string;
}

// New platforms default to YouTube so the type select and settings start on a
// valid, fully-supported option.
export function createPlatformFormModel(): PlatformFormModel {
  return {
    type: defaultPlatformType,
    name: '',
    referenceKey: '',
    titleTemplateId: '',
    descriptionTemplateId: '',
    ...youtubeSettingsFormDefaults,
    ...wordpressSettingsFormDefaults,
  };
}

// Signal Forms rules for the platform editor. Type, name, reference key, and
// publishing content always apply; provider settings rules apply only while
// their platform type is selected.
export function applyPlatformRules(path: SchemaPathTree<PlatformFormModel>): void {
  required(path.type, { message: 'Platform type is required.' });

  validate(path.name, ({ value }) =>
    value().trim().length === 0 ? { kind: 'required', message: 'Name is required.' } : undefined,
  );
  maxLength(path.name, nameMaxLength, {
    message: `Name must be at most ${nameMaxLength} characters.`,
  });

  validate(path.referenceKey, ({ value }) => {
    const referenceKey = value().trim();
    return referenceKey.length === 0 || referenceKeyPattern.test(referenceKey)
      ? undefined
      : {
          kind: 'pattern',
          message: 'Reference key must use only letters, numbers, or hyphen.',
        };
  });
  maxLength(path.referenceKey, referenceKeyMaxLength, {
    message: `Reference key must be at most ${referenceKeyMaxLength} characters.`,
  });

  validate(path.titleTemplateId, ({ value }) =>
    value().trim().length === 0
      ? { kind: 'required', message: 'Title template is required.' }
      : undefined,
  );

  validate(path.descriptionTemplateId, ({ value }) =>
    value().trim().length === 0
      ? { kind: 'required', message: 'Description template is required.' }
      : undefined,
  );

  applyWhen(path, ({ value }) => value().type === 'YouTube', applyYouTubeSettingsRules);

  applyWhen(path, ({ value }) => value().type === 'WordPress', applyWordPressSettingsRules);
}

export function toPlatformFormModel(platform: Platform): PlatformFormModel {
  const model: PlatformFormModel = {
    ...createPlatformFormModel(),
    type: platform.type,
    name: platform.name,
    referenceKey: platform.referenceKey ?? '',
    titleTemplateId: platform.publishingContent.titleTemplateId,
    descriptionTemplateId: platform.publishingContent.descriptionTemplateId,
  };

  if (platform.type === 'YouTube') {
    return withYouTubeSettingsFormModel(model, platform.publishSettings);
  }

  if (platform.type === 'WordPress') {
    return withWordPressSettingsFormModel(model, platform.publishSettings);
  }

  return model;
}

// Builds the type-specific settings, or undefined for unsupported form types.
function toPublishSettings(model: PlatformFormModel): PlatformPublishSettings | undefined {
  if (model.type === 'YouTube') {
    return toYouTubePublishSettings(model);
  }

  if (model.type !== 'WordPress') {
    return undefined;
  }

  return toWordPressPublishSettings(model);
}

function toReferenceKey(value: string): string | null {
  const referenceKey = value.trim();
  return referenceKey.length === 0 ? null : referenceKey;
}

function toPublishingContent(model: PlatformFormModel): PublishingContent {
  return {
    titleTemplateId: model.titleTemplateId.trim(),
    descriptionTemplateId: model.descriptionTemplateId.trim(),
  };
}

export function toCreatePlatformRequest(model: PlatformFormModel): CreatePlatformRequest {
  return {
    name: model.name.trim(),
    referenceKey: toReferenceKey(model.referenceKey),
    type: model.type as PlatformType,
    publishSettings: toPublishSettings(model),
    publishingContent: toPublishingContent(model),
  };
}

export function toUpdatePlatformRequest(model: PlatformFormModel): UpdatePlatformRequest {
  return {
    name: model.name.trim(),
    referenceKey: toReferenceKey(model.referenceKey),
    publishSettings: toPublishSettings(model),
    publishingContent: toPublishingContent(model),
  };
}

export function sameCreatePlatformRequest(
  left: CreatePlatformRequest,
  right: CreatePlatformRequest,
): boolean {
  return sameRequest(left, right);
}

export function sameUpdatePlatformRequest(
  left: UpdatePlatformRequest,
  right: UpdatePlatformRequest,
): boolean {
  return sameRequest(left, right);
}
