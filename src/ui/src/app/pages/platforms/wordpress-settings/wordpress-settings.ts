import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { type Field } from '@angular/forms/signals';

import { Checkbox } from 'src/app/shared/components/checkbox/checkbox';
import { Input } from 'src/app/shared/components/input/input';
import { MaskedInput } from 'src/app/shared/components/masked-input/masked-input';
import { Select, SelectOption } from 'src/app/shared/components/select/select';

/**
 * Editor settings specific to a WordPress platform. Presentational only: the
 * parent editor owns the form model, validation, and request mapping.
 */
@Component({
  selector: 'app-wordpress-settings',
  imports: [Checkbox, Input, MaskedInput, Select],
  templateUrl: './wordpress-settings.html',
  styleUrl: './wordpress-settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordPressSettings {
  readonly siteUrl = input.required<Field<string>>();
  readonly username = input.required<Field<string>>();
  readonly applicationPassword = input.required<Field<string>>();
  readonly postStatus = input.required<Field<string>>();
  readonly sticky = input.required<Field<boolean>>();
  readonly scheduleOffsetHours = input.required<Field<string>>();
  readonly passwordDisplayValue = input('');

  protected readonly postStatusOptions: readonly SelectOption[] = [
    { value: 'draft', label: 'Draft' },
    { value: 'pending', label: 'Pending' },
    { value: 'private', label: 'Private' },
    { value: 'future', label: 'Scheduled' },
    { value: 'publish', label: 'Publish' },
  ];

  protected readonly showScheduleOffset = computed(() => this.postStatus()().value() === 'future');
}
