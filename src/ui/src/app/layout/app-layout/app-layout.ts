import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AuthFacade } from 'src/app/shared/auth/auth-facade';
import { Button } from 'src/app/shared/components/button/button';
import { Toolbar } from 'src/app/shared/components/toolbar/toolbar';
import {
  ToolbarNav,
  ToolbarNavItem,
} from 'src/app/shared/components/toolbar-nav/toolbar-nav';

@Component({
  selector: 'app-layout',
  imports: [Button, RouterOutlet, Toolbar, ToolbarNav],
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

  protected isAuthenticated(): boolean {
    return this.auth.isAuthenticated();
  }

  protected async signOut(): Promise<void> {
    await this.auth.signOut();
  }
}
