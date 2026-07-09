import { maxLength, validate, type SchemaPathTree } from '@angular/forms/signals';

import {
  Platform,
  WordPressPostStatus,
  WordPressPublishSettings,
} from 'src/app/shared/api/platforms/platforms-service';
import type { PlatformFormModel } from '../platforms.form';

const siteUrlMaxLength = 2048;
const usernameMaxLength = 100;
const applicationPasswordMaxLength = 512;

export const wordpressSettingsFormDefaults: Pick<
  PlatformFormModel,
  | 'wordPressSiteUrl'
  | 'wordPressUsername'
  | 'wordPressApplicationPassword'
  | 'wordPressPostStatus'
  | 'wordPressApplicationPasswordConfigured'
  | 'wordPressPasswordDisplayValue'
> = {
  wordPressSiteUrl: '',
  wordPressUsername: '',
  wordPressApplicationPassword: '',
  wordPressPostStatus: 'draft',
  wordPressApplicationPasswordConfigured: 'false',
  wordPressPasswordDisplayValue: '',
};

export function applyWordPressSettingsRules(path: SchemaPathTree<PlatformFormModel>): void {
  validate(path.wordPressSiteUrl, ({ value }) =>
    value().trim().length === 0
      ? { kind: 'required', message: 'Site URL is required.' }
      : undefined,
  );
  maxLength(path.wordPressSiteUrl, siteUrlMaxLength, {
    message: `Site URL must be at most ${siteUrlMaxLength} characters.`,
  });

  validate(path.wordPressUsername, ({ value }) =>
    value().trim().length === 0
      ? { kind: 'required', message: 'Username is required.' }
      : undefined,
  );
  maxLength(path.wordPressUsername, usernameMaxLength, {
    message: `Username must be at most ${usernameMaxLength} characters.`,
  });

  validate(path.wordPressApplicationPassword, ({ value, valueOf }) => {
    const configured = valueOf(path.wordPressApplicationPasswordConfigured);
    if (configured !== 'true' && value().trim().length === 0) {
      return { kind: 'required', message: 'Application Password is required.' };
    }

    return undefined;
  });
  maxLength(path.wordPressApplicationPassword, applicationPasswordMaxLength, {
    message: `Application Password must be at most ${applicationPasswordMaxLength} characters.`,
  });

  validate(path.wordPressPostStatus, ({ value }) =>
    value() === 'publish' || value() === 'draft'
      ? undefined
      : { kind: 'required', message: 'Post status is required.' },
  );
}

export function toWordPressPublishSettings(model: PlatformFormModel): WordPressPublishSettings {
  const applicationPassword = model.wordPressApplicationPassword.trim();
  const settings = {
    siteUrl: model.wordPressSiteUrl.trim(),
    username: model.wordPressUsername.trim(),
    postStatus: model.wordPressPostStatus as WordPressPostStatus,
  };

  return applicationPassword.length === 0 ? settings : { ...settings, applicationPassword };
}

export function withWordPressSettingsFormModel(
  model: PlatformFormModel,
  settings: Platform['publishSettings'],
): PlatformFormModel {
  const wordPressSettings = isWordPressSettings(settings) ? settings : undefined;

  return {
    ...model,
    wordPressSiteUrl: wordPressSettings?.siteUrl ?? wordpressSettingsFormDefaults.wordPressSiteUrl,
    wordPressUsername:
      wordPressSettings?.username ?? wordpressSettingsFormDefaults.wordPressUsername,
    wordPressApplicationPassword: '',
    wordPressPostStatus:
      wordPressSettings?.postStatus ?? wordpressSettingsFormDefaults.wordPressPostStatus,
    wordPressApplicationPasswordConfigured: String(
      wordPressSettings?.applicationPasswordConfigured ?? false,
    ),
    wordPressPasswordDisplayValue:
      wordPressSettings?.passwordDisplayValue ??
      wordpressSettingsFormDefaults.wordPressPasswordDisplayValue,
  };
}

function isWordPressSettings(
  settings: Platform['publishSettings'],
): settings is WordPressPublishSettings {
  return settings !== undefined && 'siteUrl' in settings;
}
