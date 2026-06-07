import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { AuthFacade } from 'src/app/shared/auth/auth-facade';
import { Button } from 'src/app/shared/components/button/button';

@Component({
  selector: 'app-home',
  imports: [Button],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home {
  private readonly auth = inject(AuthFacade);

  protected async signIn(): Promise<void> {
    await this.auth.signIn('/calendar-events');
  }
}
