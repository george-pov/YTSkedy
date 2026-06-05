import {
  booleanAttribute,
  ChangeDetectionStrategy,
  Component,
  input,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';

type ButtonAppearance = 'text' | 'filled' | 'elevated' | 'outlined' | 'tonal';
type ButtonType = 'button' | 'submit' | 'reset';

@Component({
  selector: 'app-button',
  imports: [MatButtonModule],
  templateUrl: './button.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Button {
  readonly appearance = input<ButtonAppearance>('filled');
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly type = input<ButtonType>('button');
}
