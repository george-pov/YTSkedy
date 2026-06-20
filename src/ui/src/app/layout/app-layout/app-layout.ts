import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AuthFacade } from 'src/app/shared/auth/auth-facade';
import {
  toUserBadgeView,
  UserBadgeView,
} from 'src/app/shared/auth/user-badge-view';
import { Toolbar } from 'src/app/shared/components/toolbar/toolbar';
import {
  ToolbarNav,
  ToolbarNavItem,
} from 'src/app/shared/components/toolbar-nav/toolbar-nav';
import { UserBadge } from 'src/app/shared/components/user-badge/user-badge';

@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, Toolbar, ToolbarNav, UserBadge],
  templateUrl: './app-layout.html',
  styleUrl: './app-layout.scss',
})
export class AppLayout {
  private readonly auth = inject(AuthFacade);

  protected readonly navItems: readonly ToolbarNavItem[] = [
    { label: 'Calendar', link: '/calendar-events' },
    { label: 'Templates', link: '/templates' },
    { label: 'Settings', link: '/settings' },
  ];

  protected readonly userBadge: UserBadgeView = toUserBadgeView(
    this.auth.getUserIdentity(),
  );

  protected isAuthenticated(): boolean {
    return this.auth.isAuthenticated();
  }

  protected async signOut(): Promise<void> {
    await this.auth.signOut();
  }
}
