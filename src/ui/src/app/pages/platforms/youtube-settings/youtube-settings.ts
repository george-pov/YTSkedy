import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { type Field } from '@angular/forms/signals';

import { Input } from 'src/app/shared/components/input/input';
import { Select, SelectOption } from 'src/app/shared/components/select/select';

/**
 * Editor settings specific to a YouTube platform. Presentational only: it binds
 * the supplied Signal Forms fields to the shared form controls. The platform
 * editor owns the form model and validation; each other platform type provides
 * its own settings component (for example a future `wordpress-settings`).
 */
@Component({
  selector: 'app-youtube-settings',
  imports: [Input, Select],
  templateUrl: './youtube-settings.html',
  styleUrl: './youtube-settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class YouTubeSettings {
  /** Credential reference for the YouTube channel. */
  readonly credentials = input.required<Field<string>>();
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
