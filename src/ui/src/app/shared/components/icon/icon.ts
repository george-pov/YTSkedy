import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

export const supportedIconNames = [
  'add',
  'arrow_back',
  'check_circle',
  'close',
  'delete',
  'edit',
  'error',
  'info',
  'logout',
  'menu',
  'publish',
  'save',
  'upload',
  'visibility',
  'warning',
] as const;

export type IconName = (typeof supportedIconNames)[number];

/** Decorative Angular Material icon from the app's self-hosted symbol subset. */
@Component({
  selector: 'app-icon',
  imports: [MatIconModule],
  templateUrl: './icon.html',
  styleUrl: './icon.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    'aria-hidden': 'true',
  },
})
export class Icon {
  readonly name = input.required<IconName>();
}
