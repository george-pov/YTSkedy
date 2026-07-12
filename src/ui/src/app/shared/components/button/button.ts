import {
  booleanAttribute,
  ChangeDetectionStrategy,
  Component,
  input,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';

import {
  Icon,
  type IconName,
} from 'src/app/shared/components/icon/icon';

export type ButtonAppearance =
  | 'text'
  | 'filled'
  | 'elevated'
  | 'outlined'
  | 'tonal';
type ButtonType = 'button' | 'submit' | 'reset';

@Component({
  selector: 'app-button',
  imports: [MatButtonModule, Icon],
  templateUrl: './button.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Button {
  readonly appearance = input<ButtonAppearance>('filled');
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly type = input<ButtonType>('button');

  /** Optional app icon rendered inside the button. */
  readonly icon = input<IconName>();

  /**
   * Renders a compact, icon-only button (no projected label). Requires
   * {@link icon} for the glyph and {@link ariaLabel} for the accessible name,
   * since there is no visible text to name the control.
   */
  readonly iconButton = input(false, { transform: booleanAttribute });

  /**
   * Accessible name for the button. Required when {@link iconButton} is set and
   * otherwise optional; applied as `aria-label` when provided.
   */
  readonly ariaLabel = input<string>();
}
