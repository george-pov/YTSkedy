import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { type Field } from '@angular/forms/signals';

import { Input } from 'src/app/shared/components/input/input';
import { Select, SelectOption } from 'src/app/shared/components/select/select';

/**
 * Editor settings specific to a WordPress platform. Presentational only: the
 * parent editor owns the form model, validation, and request mapping.
 */
@Component({
  selector: 'app-wordpress-settings',
  imports: [Input, Select],
  templateUrl: './wordpress-settings.html',
  styleUrl: './wordpress-settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordPressSettings {
  readonly siteUrl = input.required<Field<string>>();
  readonly username = input.required<Field<string>>();
  readonly applicationPassword = input.required<Field<string>>();
  readonly postStatus = input.required<Field<string>>();
  readonly applicationPasswordConfigured = input(false);

  protected readonly postStatusOptions: readonly SelectOption[] = [
    { value: 'draft', label: 'Draft' },
    { value: 'publish', label: 'Publish' },
  ];
}
