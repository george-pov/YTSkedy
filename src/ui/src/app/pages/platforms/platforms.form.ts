import {
  applyWhen,
  maxLength,
  required,
  validate,
  type SchemaPathTree,
} from '@angular/forms/signals';

import {
  CreatePlatformRequest,
  PlatformPublishSettings,
  PublishingContent,
  PlatformType,
  UpdatePlatformRequest,
  YouTubeCredentials,
  YouTubePrivacyStatus,
  YouTubePublishSettings,
  WordPressPostStatus,
} from 'src/app/shared/api/platforms/platforms-service';

export const nameMaxLength = 50;
export const referenceKeyMaxLength = 15;
export const referenceKeyPattern = /^[A-Za-z0-9-]*$/;
export const youTubeClientIdMaxLength = 256;
export const youTubeClientSecretMaxLength = 256;
export const youTubeRefreshTokenMaxLength = 2048;
export const wordPressSiteUrlMaxLength = 2048;
export const wordPressUsernameMaxLength = 100;
export const wordPressApplicationPasswordMaxLength = 512;

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
  wordPressSiteUrl: string;
  wordPressUsername: string;
  wordPressApplicationPassword: string;
  wordPressPostStatus: string;
  wordPressApplicationPasswordConfigured: string;
  wordPressPasswordDisplayValue: string;
}

// New platforms default to YouTube so the type select and settings start on a
// valid, fully-supported option.
export function createPlatformFormModel(): PlatformFormModel {
  return {
    type: 'YouTube',
    name: '',
    referenceKey: '',
    titleTemplateId: '',
    descriptionTemplateId: '',
    youTubeClientId: '',
    youTubeClientSecret: '',
    youTubeRefreshToken: '',
    youTubeClientSecretConfigured: 'false',
    youTubeRefreshTokenConfigured: 'false',
    youTubeClientSecretDisplayValue: '',
    youTubeRefreshTokenDisplayValue: '',
    youTubePrivacyStatus: 'private',
    youTubeMadeForKids: 'false',
    wordPressSiteUrl: '',
    wordPressUsername: '',
    wordPressApplicationPassword: '',
    wordPressPostStatus: 'draft',
    wordPressApplicationPasswordConfigured: 'false',
    wordPressPasswordDisplayValue: '',
  };
}

// Signal Forms rules for the platform editor. Type and name always apply; the
// YouTube settings rules apply only while the selected type is YouTube so other
// types (whose settings are not built yet) can still be saved.
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

  applyWhen(
    path,
    ({ value }) => value().type === 'YouTube',
    (youTubePath) => {
      validate(youTubePath.youTubeClientId, ({ value }) =>
        value().trim().length === 0
          ? { kind: 'required', message: 'Client ID is required.' }
          : undefined,
      );
      maxLength(youTubePath.youTubeClientId, youTubeClientIdMaxLength, {
        message: `Client ID must be at most ${youTubeClientIdMaxLength} characters.`,
      });

      validate(youTubePath.youTubeClientSecret, ({ value, valueOf }) => {
        const configured = valueOf(youTubePath.youTubeClientSecretConfigured);
        if (configured !== 'true' && value().trim().length === 0) {
          return { kind: 'required', message: 'Client secret is required.' };
        }

        return undefined;
      });
      maxLength(youTubePath.youTubeClientSecret, youTubeClientSecretMaxLength, {
        message: `Client secret must be at most ${youTubeClientSecretMaxLength} characters.`,
      });

      validate(youTubePath.youTubeRefreshToken, ({ value, valueOf }) => {
        const configured = valueOf(youTubePath.youTubeRefreshTokenConfigured);
        if (configured !== 'true' && value().trim().length === 0) {
          return { kind: 'required', message: 'Refresh token is required.' };
        }

        return undefined;
      });
      maxLength(youTubePath.youTubeRefreshToken, youTubeRefreshTokenMaxLength, {
        message: `Refresh token must be at most ${youTubeRefreshTokenMaxLength} characters.`,
      });
    },
  );

  applyWhen(
    path,
    ({ value }) => value().type === 'WordPress',
    (wordPressPath) => {
      validate(wordPressPath.wordPressSiteUrl, ({ value }) =>
        value().trim().length === 0
          ? { kind: 'required', message: 'Site URL is required.' }
          : undefined,
      );
      maxLength(wordPressPath.wordPressSiteUrl, wordPressSiteUrlMaxLength, {
        message: `Site URL must be at most ${wordPressSiteUrlMaxLength} characters.`,
      });

      validate(wordPressPath.wordPressUsername, ({ value }) =>
        value().trim().length === 0
          ? { kind: 'required', message: 'Username is required.' }
          : undefined,
      );
      maxLength(wordPressPath.wordPressUsername, wordPressUsernameMaxLength, {
        message: `Username must be at most ${wordPressUsernameMaxLength} characters.`,
      });

      validate(wordPressPath.wordPressApplicationPassword, ({ value, valueOf }) => {
        const configured = valueOf(wordPressPath.wordPressApplicationPasswordConfigured);
        if (configured !== 'true' && value().trim().length === 0) {
          return { kind: 'required', message: 'Application Password is required.' };
        }

        return undefined;
      });
      maxLength(wordPressPath.wordPressApplicationPassword, wordPressApplicationPasswordMaxLength, {
        message: `Application Password must be at most ${wordPressApplicationPasswordMaxLength} characters.`,
      });

      validate(wordPressPath.wordPressPostStatus, ({ value }) =>
        value() === 'publish' || value() === 'draft'
          ? undefined
          : { kind: 'required', message: 'Post status is required.' },
      );
    },
  );
}

// Builds the type-specific settings, or undefined for types without modeled
// settings yet (currently anything other than YouTube).
function toPublishSettings(model: PlatformFormModel): PlatformPublishSettings | undefined {
  if (model.type === 'YouTube') {
    const credentials: YouTubeCredentials = {
      clientId: model.youTubeClientId.trim(),
    };
    const clientSecret = model.youTubeClientSecret.trim();
    if (clientSecret.length > 0) {
      credentials.clientSecret = clientSecret;
    }

    const refreshToken = model.youTubeRefreshToken.trim();
    if (refreshToken.length > 0) {
      credentials.refreshToken = refreshToken;
    }

    return {
      credentials,
      privacyStatus: model.youTubePrivacyStatus as YouTubePrivacyStatus,
      selfDeclaredMadeForKids: model.youTubeMadeForKids === 'true',
    };
  }

  if (model.type !== 'WordPress') {
    return undefined;
  }

  const applicationPassword = model.wordPressApplicationPassword.trim();
  const settings = {
    siteUrl: model.wordPressSiteUrl.trim(),
    username: model.wordPressUsername.trim(),
    postStatus: model.wordPressPostStatus as WordPressPostStatus,
  };

  return applicationPassword.length === 0 ? settings : { ...settings, applicationPassword };
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
