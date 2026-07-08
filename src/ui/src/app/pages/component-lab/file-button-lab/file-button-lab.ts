import { ChangeDetectionStrategy, Component, signal } from '@angular/core';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { FileButton } from 'src/app/shared/components/file-button/file-button';

@Component({
  selector: 'app-file-button-lab',
  imports: [FileButton, LabExample, LabPage],
  templateUrl: './file-button-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FileButtonLab {
  protected readonly selectedFile = signal<File | null>(null);

  protected selectFile(file: File): void {
    this.selectedFile.set(file);
  }
}
