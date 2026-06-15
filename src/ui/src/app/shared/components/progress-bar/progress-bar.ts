import {
  ChangeDetectionStrategy,
  Component,
  input,
  numberAttribute,
} from '@angular/core';
import { MatProgressBarModule } from '@angular/material/progress-bar';

export type ProgressBarMode = 'determinate' | 'indeterminate';

/**
 * Thin wrapper over Angular Material's progress bar for app use. Defaults to
 * `indeterminate`, the page-load case where the duration is unknown (for
 * example while a list is being fetched). Set `mode` to `determinate` and bind
 * `value` (0-100) to show known progress such as an upload.
 *
 * The bar carries `role="progressbar"` but no visible text, so always pass a
 * `label`; it is the accessible name announced to assistive technology.
 */
@Component({
  selector: 'app-progress-bar',
  imports: [MatProgressBarModule],
  templateUrl: './progress-bar.html',
  styleUrl: './progress-bar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProgressBar {
  readonly mode = input<ProgressBarMode>('indeterminate');

  /** Completion percentage (0-100). Used only in `determinate` mode. */
  readonly value = input(0, { transform: numberAttribute });

  /** Accessible name announced for the progress bar. */
  readonly label = input<string>('');
}
