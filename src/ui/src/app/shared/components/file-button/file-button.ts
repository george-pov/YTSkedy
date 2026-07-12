import {
  booleanAttribute,
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';

import {
  Button,
  type ButtonAppearance,
} from 'src/app/shared/components/button/button';
import { type IconName } from 'src/app/shared/components/icon/icon';

@Component({
  selector: 'app-file-button',
  imports: [Button],
  templateUrl: './file-button.html',
  styleUrl: './file-button.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FileButton {
  readonly label = input.required<string>();
  readonly accept = input('');
  readonly appearance = input<ButtonAppearance>('filled');
  readonly icon = input<IconName>('upload');
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly fileSelected = output<File>();

  protected openPicker(input: HTMLInputElement): void {
    if (this.disabled()) {
      return;
    }

    input.click();
  }

  protected onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    const file = input?.files?.item(0) ?? null;
    if (file === null) {
      return;
    }

    this.fileSelected.emit(file);

    if (input !== null) {
      input.value = '';
    }
  }
}
