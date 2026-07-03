import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { type Field } from '@angular/forms/signals';

import { Input } from 'src/app/shared/components/input/input';
import { Select, SelectOption } from 'src/app/shared/components/select/select';

/**
 * Editor settings specific to a YouTube platform. Presentational only: the
 * parent editor owns the form model, validation, and request mapping.
 */
@Component({
  selector: 'app-youtube-settings',
  imports: [Input, Select],
  templateUrl: './youtube-settings.html',
  styleUrl: './youtube-settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class YouTubeSettings {
  readonly clientId = input.required<Field<string>>();
  readonly clientSecret = input.required<Field<string>>();
  readonly refreshToken = input.required<Field<string>>();
  readonly clientSecretConfigured = input(false);
  readonly refreshTokenConfigured = input(false);
  readonly clientSecretDisplayValue = input('');
  readonly refreshTokenDisplayValue = input('');
  /** Broadcast visibility: `private`, `public`, or `unlisted`. */
  readonly privacyStatus = input.required<Field<string>>();
  /** Self-declared "made for kids" flag, carried as `'true'`/`'false'`. */
  readonly madeForKids = input.required<Field<string>>();

  protected readonly privacyOptions: readonly SelectOption[] = [
    { value: 'private', label: 'Private' },
    { value: 'public', label: 'Public' },
    { value: 'unlisted', label: 'Unlisted' },
  ];

  protected readonly madeForKidsOptions: readonly SelectOption[] = [
    { value: 'false', label: 'No' },
    { value: 'true', label: 'Yes' },
  ];
}
