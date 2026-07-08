import { ChangeDetectionStrategy, Component, input } from '@angular/core';

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
}
