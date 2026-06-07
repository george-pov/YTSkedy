import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AuthFacade } from 'src/app/shared/auth/auth-facade';
import { Button } from 'src/app/shared/components/button/button';
import { Toolbar } from 'src/app/shared/components/toolbar/toolbar';

@Component({
  selector: 'app-layout',
  imports: [Button, RouterOutlet, Toolbar],
  templateUrl: './app-layout.html',
  styleUrl: './app-layout.scss',
})
export class AppLayout {
  private readonly auth = inject(AuthFacade);

  protected isAuthenticated(): boolean {
    return this.auth.isAuthenticated();
  }

  protected async signOut(): Promise<void> {
    await this.auth.signOut();
  }
}
