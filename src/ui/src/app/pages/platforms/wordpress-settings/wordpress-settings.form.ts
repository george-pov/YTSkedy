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
const minimumScheduleOffsetHours = 1;
const maximumScheduleOffsetHours = 168;
const wholeHoursPattern = /^\d+$/;

export const wordpressSettingsFormDefaults: Pick<
  PlatformFormModel,
  | 'wordPressSiteUrl'
  | 'wordPressUsername'
  | 'wordPressApplicationPassword'
  | 'wordPressPostStatus'
  | 'wordPressCategoryIds'
  | 'wordPressSticky'
  | 'wordPressScheduleOffsetHours'
  | 'wordPressApplicationPasswordConfigured'
  | 'wordPressPasswordDisplayValue'
> = {
  wordPressSiteUrl: '',
  wordPressUsername: '',
  wordPressApplicationPassword: '',
  wordPressPostStatus: 'draft',
  wordPressCategoryIds: [],
  wordPressSticky: false,
  wordPressScheduleOffsetHours: '',
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
    ['draft', 'pending', 'private', 'future', 'publish'].includes(value())
      ? undefined
      : { kind: 'required', message: 'Post status is required.' },
  );

  validate(path.wordPressScheduleOffsetHours, ({ value, valueOf }) => {
    if (valueOf(path.wordPressPostStatus) !== 'future') {
      return undefined;
    }

    const offset = value().trim();
    if (offset.length === 0) {
      return {
        kind: 'required',
        message: 'Hours before event start is required for Scheduled posts.',
      };
    }

    return parseScheduleOffsetHours(offset) === undefined
      ? {
          kind: 'pattern',
          message: 'Hours before event start must be a whole number from 1 through 168.',
        }
      : undefined;
  });
}

export function toWordPressPublishSettings(model: PlatformFormModel): WordPressPublishSettings {
  const applicationPassword = model.wordPressApplicationPassword.trim();
  const settings = {
    siteUrl: model.wordPressSiteUrl.trim(),
    username: model.wordPressUsername.trim(),
    postStatus: model.wordPressPostStatus as WordPressPostStatus,
    categoryIds: [...model.wordPressCategoryIds],
    sticky: model.wordPressSticky,
  };

  const scheduleOffsetHours = toScheduleOffsetHours(model);
  const scheduledSettings =
    scheduleOffsetHours === undefined ? settings : { ...settings, scheduleOffsetHours };

  return applicationPassword.length === 0
    ? scheduledSettings
    : { ...scheduledSettings, applicationPassword };
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
    wordPressCategoryIds:
      wordPressSettings === undefined
        ? [...wordpressSettingsFormDefaults.wordPressCategoryIds]
        : [...wordPressSettings.categoryIds],
    wordPressSticky: wordPressSettings?.sticky ?? wordpressSettingsFormDefaults.wordPressSticky,
    wordPressScheduleOffsetHours:
      wordPressSettings?.scheduleOffsetHours?.toString() ??
      wordpressSettingsFormDefaults.wordPressScheduleOffsetHours,
    wordPressApplicationPasswordConfigured: String(
      wordPressSettings?.applicationPasswordConfigured ?? false,
    ),
    wordPressPasswordDisplayValue:
      wordPressSettings?.passwordDisplayValue ??
      wordpressSettingsFormDefaults.wordPressPasswordDisplayValue,
  };
}

function toScheduleOffsetHours(model: PlatformFormModel): number | undefined {
  if (model.wordPressPostStatus !== 'future') {
    return undefined;
  }

  return parseScheduleOffsetHours(model.wordPressScheduleOffsetHours.trim());
}

function parseScheduleOffsetHours(value: string): number | undefined {
  if (!wholeHoursPattern.test(value)) {
    return undefined;
  }

  const hours = Number(value);
  return hours >= minimumScheduleOffsetHours && hours <= maximumScheduleOffsetHours
    ? hours
    : undefined;
}

function isWordPressSettings(
  settings: Platform['publishSettings'],
): settings is WordPressPublishSettings {
  return settings !== undefined && 'siteUrl' in settings;
}
