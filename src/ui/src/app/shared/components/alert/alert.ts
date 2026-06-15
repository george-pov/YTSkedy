import {
  booleanAttribute,
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

export type AlertVariant = 'success' | 'error' | 'info' | 'warning';

/**
 * Inline, non-blocking status banner for messages tied to a place on the page
 * (a failed load, a validation summary, a soft confirmation). Transient,
 * page-agnostic feedback such as a success toast should use the snackbar
 * notification service instead.
 *
 * The message is projected as content. `error` and `warning` are announced
 * assertively (`role="alert"`); `success` and `info` are announced politely
 * (`role="status"`). An icon accompanies the color so the meaning is not
 * conveyed by color alone. When `dismissible` is set the banner renders a close
 * control and emits {@link dismissed}; the host decides whether to hide it.
 */
@Component({
  selector: 'app-alert',
  imports: [MatIconModule],
  templateUrl: './alert.html',
  styleUrl: './alert.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'variant()',
    '[attr.role]': 'role()',
  },
})
export class Alert {
  readonly variant = input<AlertVariant>('info');
  readonly dismissible = input(false, { transform: booleanAttribute });

  /** Emitted when the user activates the close control. */
  readonly dismissed = output<void>();

  // Errors and warnings interrupt assistive technology; success and info are
  // announced without interrupting the current task.
  protected readonly role = computed(() =>
    this.variant() === 'error' || this.variant() === 'warning'
      ? 'alert'
      : 'status',
  );

  protected readonly icon = computed(() => variantIcons[this.variant()]);

  protected dismiss(): void {
    this.dismissed.emit();
  }
}

const variantIcons: Record<AlertVariant, string> = {
  success: 'check_circle',
  error: 'error',
  info: 'info',
  warning: 'warning',
};
