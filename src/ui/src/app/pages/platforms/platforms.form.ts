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
  PlatformType,
  UpdatePlatformRequest,
  YouTubePrivacyStatus,
  YouTubePublishSettings,
  WordPressPostStatus,
} from 'src/app/shared/api/platforms/platforms-service';

export const nameMaxLength = 50;
export const credentialsMaxLength = 100;
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
  youTubeCredentials: string;
  youTubePrivacyStatus: string;
  youTubeMadeForKids: string;
  wordPressSiteUrl: string;
  wordPressUsername: string;
  wordPressApplicationPassword: string;
  wordPressPostStatus: string;
  wordPressApplicationPasswordConfigured: string;
}

// New platforms default to YouTube so the type select and settings start on a
// valid, fully-supported option.
export function createPlatformFormModel(): PlatformFormModel {
  return {
    type: 'YouTube',
    name: '',
    youTubeCredentials: '',
    youTubePrivacyStatus: 'private',
    youTubeMadeForKids: 'false',
    wordPressSiteUrl: '',
    wordPressUsername: '',
    wordPressApplicationPassword: '',
    wordPressPostStatus: 'draft',
    wordPressApplicationPasswordConfigured: 'false',
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

  applyWhen(
    path,
    ({ value }) => value().type === 'YouTube',
    (youTubePath) => {
      validate(youTubePath.youTubeCredentials, ({ value }) =>
        value().trim().length === 0
          ? { kind: 'required', message: 'Credentials are required.' }
          : undefined,
      );
      maxLength(youTubePath.youTubeCredentials, credentialsMaxLength, {
        message: `Credentials must be at most ${credentialsMaxLength} characters.`,
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
    return {
      credentials: model.youTubeCredentials.trim(),
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

export function toCreatePlatformRequest(model: PlatformFormModel): CreatePlatformRequest {
  return {
    name: model.name.trim(),
    type: model.type as PlatformType,
    publishSettings: toPublishSettings(model),
  };
}

export function toUpdatePlatformRequest(model: PlatformFormModel): UpdatePlatformRequest {
  return {
    name: model.name.trim(),
    publishSettings: toPublishSettings(model),
  };
}
