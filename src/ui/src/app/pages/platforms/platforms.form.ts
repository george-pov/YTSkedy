import {
  applyWhen,
  maxLength,
  required,
  validate,
  type SchemaPathTree,
} from '@angular/forms/signals';

import {
  CreatePlatformRequest,
  PlatformType,
  UpdatePlatformRequest,
  YouTubePrivacyStatus,
  YouTubePublishSettings,
} from 'src/app/shared/api/platforms/platforms-service';

export const nameMaxLength = 50;
export const credentialsMaxLength = 100;

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
  };
}

// Signal Forms rules for the platform editor. Type and name always apply; the
// YouTube settings rules apply only while the selected type is YouTube so other
// types (whose settings are not built yet) can still be saved.
export function applyPlatformRules(path: SchemaPathTree<PlatformFormModel>): void {
  required(path.type, { message: 'Platform type is required.' });

  validate(path.name, ({ value }) =>
    value().trim().length === 0
      ? { kind: 'required', message: 'Name is required.' }
      : undefined,
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
}

// Builds the type-specific settings, or undefined for types without modeled
// settings yet (currently anything other than YouTube).
function toPublishSettings(
  model: PlatformFormModel,
): YouTubePublishSettings | undefined {
  if (model.type !== 'YouTube') {
    return undefined;
  }

  return {
    credentials: model.youTubeCredentials.trim(),
    privacyStatus: model.youTubePrivacyStatus as YouTubePrivacyStatus,
    selfDeclaredMadeForKids: model.youTubeMadeForKids === 'true',
  };
}

export function toCreatePlatformRequest(
  model: PlatformFormModel,
): CreatePlatformRequest {
  return {
    name: model.name.trim(),
    type: model.type as PlatformType,
    publishSettings: toPublishSettings(model),
  };
}

export function toUpdatePlatformRequest(
  model: PlatformFormModel,
): UpdatePlatformRequest {
  return {
    name: model.name.trim(),
    publishSettings: toPublishSettings(model),
  };
}
