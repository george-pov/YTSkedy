import { map } from 'rxjs';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  output,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';

/**
 * Pure presentational user badge: a monogram circle plus the full name, with a
 * menu offering Sign Out. It renders exactly the values it is given and holds no
 * auth or routing logic. The consumer supplies the monogram text (e.g. "JD", or
 * "NA" when the name is unknown) and handles the {@link signOut} event.
 *
 * Below the `sm` breakpoint the name is hidden and a tooltip carries the full
 * name instead, so the badge collapses to just the monogram.
 */
@Component({
  selector: 'app-user-badge',
  imports: [MatIconModule, MatMenuModule, MatTooltipModule],
  templateUrl: './user-badge.html',
  styleUrl: './user-badge.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserBadge {
  /** Pre-computed monogram text shown in the circle, e.g. "JD" or "NA". */
  readonly initials = input.required<string>();

  /** Full display name shown beside the monogram and in the menu header. */
  readonly fullName = input.required<string>();

  /** Emitted when the user activates Sign Out. */
  readonly signOut = output<void>();

  private readonly breakpoints = inject(BreakpointObserver);

  // Collapse to monogram-only below the `sm` breakpoint. The same signal drives
  // the tooltip so the full name is only offered when its label is hidden.
  protected readonly isCompact = toSignal(
    this.breakpoints
      .observe(Breakpoints.XSmall)
      .pipe(map((state) => state.matches)),
    { initialValue: false },
  );
}
