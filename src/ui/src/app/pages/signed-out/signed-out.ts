import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';

import { Button } from 'src/app/shared/components/button/button';

@Component({
  selector: 'app-signed-out',
  imports: [Button],
  templateUrl: './signed-out.html',
  styleUrl: './signed-out.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignedOut {
  private readonly router = inject(Router);

  protected async returnHome(): Promise<void> {
    await this.router.navigateByUrl('/');
  }
}
