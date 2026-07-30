import {
  booleanAttribute,
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';

import { Icon, type IconName } from 'src/app/shared/components/icon/icon';

export type ButtonAppearance = 'text' | 'filled' | 'elevated' | 'outlined' | 'tonal';
export type ButtonVariant = ButtonAppearance | 'icon' | 'danger-filled';
export type LabeledButtonVariant = Exclude<ButtonVariant, 'icon'>;
type ButtonType = 'button' | 'submit' | 'reset';

@Component({
  selector: 'app-button',
  imports: [MatButtonModule, Icon],
  templateUrl: './button.html',
  styleUrl: './button.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Button {
  readonly variant = input<ButtonVariant>('filled');
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly type = input<ButtonType>('button');

  /** Optional app icon rendered inside the button. */
  readonly icon = input<IconName>();

  /**
   * Accessible name for the button. Required when {@link variant} is `icon`
   * and otherwise optional; applied as `aria-label` when provided.
   */
  readonly ariaLabel = input<string>();

  protected readonly isIconButton = computed(() => this.variant() === 'icon');
  protected readonly isDanger = computed(() => this.variant() === 'danger-filled');
  protected readonly appearance = computed<ButtonAppearance>(() => {
    const variant = this.variant();
    return variant === 'danger-filled' || variant === 'icon' ? 'filled' : variant;
  });
}
