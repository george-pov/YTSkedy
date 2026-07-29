import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink, type UrlTree } from '@angular/router';

import { type ButtonAppearance } from 'src/app/shared/components/button/button';
import { Icon, type IconName } from 'src/app/shared/components/icon/icon';

export type ButtonLinkTarget = string | readonly unknown[] | UrlTree;

@Component({
  selector: 'app-button-link',
  imports: [RouterLink, MatButtonModule, Icon],
  templateUrl: './button-link.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ButtonLink {
  readonly route = input.required<ButtonLinkTarget>();
  readonly appearance = input<ButtonAppearance>('text');
  readonly icon = input<IconName>();
}
