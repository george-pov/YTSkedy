import { Type } from '@angular/core';

import { AlertLab } from './alert-lab/alert-lab';
import { ButtonLab } from './button-lab/button-lab';
import { CheckboxLab } from './checkbox-lab/checkbox-lab';
import { ChipListLab } from './chip-list-lab/chip-list-lab';
import { ConfirmationDialogLab } from './confirmation-dialog-lab/confirmation-dialog-lab';
import { DataTableLab } from './data-table-lab/data-table-lab';
import { FileButtonLab } from './file-button-lab/file-button-lab';
import { FormControlsLab } from './form-controls-lab/form-controls-lab';
import { InputLab } from './input-lab/input-lab';
import { MaskedInputLab } from './masked-input-lab/masked-input-lab';
import { ProgressBarLab } from './progress-bar-lab/progress-bar-lab';
import { StatusPillLab } from './status-pill-lab/status-pill-lab';
import { ToolbarLab } from './toolbar-lab/toolbar-lab';
import { ToolbarNavLab } from './toolbar-nav-lab/toolbar-nav-lab';
import { UserBadgeLab } from './user-badge-lab/user-badge-lab';

export interface ComponentLabItem {
  readonly label: string;
  readonly component: Type<unknown>;
}

export const componentLabItems: readonly ComponentLabItem[] = [
  {
    label: 'Toolbar',
    component: ToolbarLab,
  },
  {
    label: 'Toolbar Nav',
    component: ToolbarNavLab,
  },
  {
    label: 'Button',
    component: ButtonLab,
  },
  {
    label: 'File Button',
    component: FileButtonLab,
  },
  {
    label: 'Form Controls',
    component: FormControlsLab,
  },
  {
    label: 'Input',
    component: InputLab,
  },
  {
    label: 'Checkbox',
    component: CheckboxLab,
  },
  {
    label: 'Chip List',
    component: ChipListLab,
  },
  {
    label: 'Masked Input',
    component: MaskedInputLab,
  },
  {
    label: 'Data Table',
    component: DataTableLab,
  },
  {
    label: 'Alert',
    component: AlertLab,
  },
  {
    label: 'Status Pill',
    component: StatusPillLab,
  },
  {
    label: 'Confirmation Dialog',
    component: ConfirmationDialogLab,
  },
  {
    label: 'Progress Bar',
    component: ProgressBarLab,
  },
  {
    label: 'User Badge',
    component: UserBadgeLab,
  },
];
