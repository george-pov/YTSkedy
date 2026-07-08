import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';

import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { FileButton } from 'src/app/shared/components/file-button/file-button';
import { StatusPill } from 'src/app/shared/components/status-pill/status-pill';
import { ThumbnailEditorState } from './thumbnail-editor.state';

@Component({
  selector: 'app-thumbnail-editor',
  imports: [Alert, Button, FileButton, StatusPill],
  templateUrl: './thumbnail-editor.html',
  styleUrl: './thumbnail-editor.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ThumbnailEditor {
  readonly state = input.required<ThumbnailEditorState>();
  readonly isEditMode = input.required<boolean>();

  protected readonly currentThumbnail = computed(() =>
    this.isEditMode() ? this.state().thumbnail() : null,
  );

  protected readonly showPicker = computed(() =>
    this.isEditMode()
      ? this.state().thumbnail() === null
      : this.state().selectedPreviewUrl() === null,
  );

  protected readonly pickerControlClass = computed(() =>
    this.isEditMode() ? 'thumbnail-replace-input' : 'thumbnail-select-input',
  );

  protected readonly pickerLabel = computed(() =>
    this.isEditMode() ? 'Add thumbnail' : 'Choose image',
  );

  protected selectFromPicker(file: File): void {
    if (this.isEditMode()) {
      this.state().replaceThumbnail(file);
      return;
    }

    this.state().selectThumbnail(file);
  }
}
