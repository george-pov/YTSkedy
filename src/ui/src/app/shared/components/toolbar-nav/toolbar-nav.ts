import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { Toolbar } from 'src/app/shared/components/toolbar/toolbar';

/**
 * A single horizontal menu item. Provide `link` for a router navigation item or
 * `action` for a button that runs a callback. `link` takes precedence when both
 * are set.
 */
export interface ToolbarNavItem {
  /** Visible menu label. */
  readonly label: string;
  /** Router link target for navigation items. */
  readonly link?: string | readonly unknown[];
  /** Match `link` exactly when deciding the active item. Defaults to false. */
  readonly exact?: boolean;
  /** Callback invoked when an action item (one without `link`) is activated. */
  readonly action?: () => void;
}

/** Horizontal alignment of the packed menu items within the toolbar row. */
export type ToolbarNavAlign = 'start' | 'end';

/**
 * Reusable horizontal menu rendered as a toolbar row. Items are router links or
 * action buttons packed together with a small gap, aligned to the start (left)
 * or end (right) of the row via the `align` input. No component-level media
 * queries: items wrap when the row runs out of space.
 */
@Component({
  selector: 'app-toolbar-nav',
  imports: [MatButtonModule, RouterLink, RouterLinkActive, Toolbar],
  templateUrl: './toolbar-nav.html',
  styleUrl: './toolbar-nav.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToolbarNav {
  /** Accessible name for the navigation landmark. */
  readonly label = input.required<string>();

  /** Menu items rendered left to right. */
  readonly items = input.required<readonly ToolbarNavItem[]>();

  /** Pack items to the start (left) or end (right) of the row. Defaults to start. */
  readonly align = input<ToolbarNavAlign>('start');
}
