import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type StatusPillVariant = 'neutral' | 'success' | 'warning';

@Component({
  selector: 'app-status-pill',
  templateUrl: './status-pill.html',
  styleUrl: './status-pill.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'variant()',
  },
})
export class StatusPill {
  readonly variant = input<StatusPillVariant>('neutral');
}
