import { maxLength, validate, type SchemaPathTree } from '@angular/forms/signals';

import {
  Platform,
  YouTubeCredentials,
  YouTubePrivacyStatus,
  YouTubePublishSettings,
} from 'src/app/shared/api/platforms/platforms-service';
import type { PlatformFormModel } from '../platforms.form';

const clientIdMaxLength = 256;
const clientSecretMaxLength = 256;
const refreshTokenMaxLength = 2048;

export const youtubeSettingsFormDefaults: Pick<
  PlatformFormModel,
  | 'youTubeClientId'
  | 'youTubeClientSecret'
  | 'youTubeRefreshToken'
  | 'youTubeClientSecretConfigured'
  | 'youTubeRefreshTokenConfigured'
  | 'youTubeClientSecretDisplayValue'
  | 'youTubeRefreshTokenDisplayValue'
  | 'youTubePrivacyStatus'
  | 'youTubeMadeForKids'
  | 'youTubeCategoryId'
  | 'youTubeContainsSyntheticMedia'
> = {
  youTubeClientId: '',
  youTubeClientSecret: '',
  youTubeRefreshToken: '',
  youTubeClientSecretConfigured: 'false',
  youTubeRefreshTokenConfigured: 'false',
  youTubeClientSecretDisplayValue: '',
  youTubeRefreshTokenDisplayValue: '',
  youTubePrivacyStatus: 'private',
  youTubeMadeForKids: 'false',
  youTubeCategoryId: '',
  youTubeContainsSyntheticMedia: 'false',
};

export function applyYouTubeSettingsRules(path: SchemaPathTree<PlatformFormModel>): void {
  validate(path.youTubeClientId, ({ value }) =>
    value().trim().length === 0
      ? { kind: 'required', message: 'Client ID is required.' }
      : undefined,
  );
  maxLength(path.youTubeClientId, clientIdMaxLength, {
    message: `Client ID must be at most ${clientIdMaxLength} characters.`,
  });

  validate(path.youTubeClientSecret, ({ value, valueOf }) => {
    const configured = valueOf(path.youTubeClientSecretConfigured);
    if (configured !== 'true' && value().trim().length === 0) {
      return { kind: 'required', message: 'Client secret is required.' };
    }

    return undefined;
  });
  maxLength(path.youTubeClientSecret, clientSecretMaxLength, {
    message: `Client secret must be at most ${clientSecretMaxLength} characters.`,
  });

  validate(path.youTubeRefreshToken, ({ value, valueOf }) => {
    const configured = valueOf(path.youTubeRefreshTokenConfigured);
    if (configured !== 'true' && value().trim().length === 0) {
      return { kind: 'required', message: 'Refresh token is required.' };
    }

    return undefined;
  });
  maxLength(path.youTubeRefreshToken, refreshTokenMaxLength, {
    message: `Refresh token must be at most ${refreshTokenMaxLength} characters.`,
  });
}

export function toYouTubePublishSettings(model: PlatformFormModel): YouTubePublishSettings {
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

  const categoryId = model.youTubeCategoryId.trim();

  return {
    credentials,
    privacyStatus: model.youTubePrivacyStatus as YouTubePrivacyStatus,
    selfDeclaredMadeForKids: model.youTubeMadeForKids === 'true',
    categoryId: categoryId.length === 0 ? null : categoryId,
    containsSyntheticMedia: model.youTubeContainsSyntheticMedia === 'true',
  };
}

export function withYouTubeSettingsFormModel(
  model: PlatformFormModel,
  settings: Platform['publishSettings'],
): PlatformFormModel {
  const youTubeSettings = isYouTubeSettings(settings) ? settings : undefined;

  return {
    ...model,
    youTubeClientId:
      youTubeSettings?.credentials.clientId ?? youtubeSettingsFormDefaults.youTubeClientId,
    youTubeClientSecret: '',
    youTubeRefreshToken: '',
    youTubeClientSecretConfigured: String(
      youTubeSettings?.credentials.clientSecretConfigured ?? false,
    ),
    youTubeRefreshTokenConfigured: String(
      youTubeSettings?.credentials.refreshTokenConfigured ?? false,
    ),
    youTubeClientSecretDisplayValue:
      youTubeSettings?.credentials.clientSecretDisplayValue ??
      youtubeSettingsFormDefaults.youTubeClientSecretDisplayValue,
    youTubeRefreshTokenDisplayValue:
      youTubeSettings?.credentials.refreshTokenDisplayValue ??
      youtubeSettingsFormDefaults.youTubeRefreshTokenDisplayValue,
    youTubePrivacyStatus:
      youTubeSettings?.privacyStatus ?? youtubeSettingsFormDefaults.youTubePrivacyStatus,
    youTubeMadeForKids: String(
      youTubeSettings?.selfDeclaredMadeForKids ?? youtubeSettingsFormDefaults.youTubeMadeForKids,
    ),
    youTubeCategoryId: youTubeSettings?.categoryId ?? youtubeSettingsFormDefaults.youTubeCategoryId,
    youTubeContainsSyntheticMedia: String(
      youTubeSettings?.containsSyntheticMedia ??
        youtubeSettingsFormDefaults.youTubeContainsSyntheticMedia,
    ),
  };
}

function isYouTubeSettings(
  settings: Platform['publishSettings'],
): settings is YouTubePublishSettings {
  return settings !== undefined && 'credentials' in settings;
}
