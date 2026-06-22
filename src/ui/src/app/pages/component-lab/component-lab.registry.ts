import { Type } from '@angular/core';

import { AlertLab } from './alert-lab/alert-lab';
import { ButtonLab } from './button-lab/button-lab';
import { ConfirmationDialogLab } from './confirmation-dialog-lab/confirmation-dialog-lab';
import { DataTableLab } from './data-table-lab/data-table-lab';
import { FormControlsLab } from './form-controls-lab/form-controls-lab';
import { InputLab } from './input-lab/input-lab';
import { ProgressBarLab } from './progress-bar-lab/progress-bar-lab';
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
    label: 'Form Controls',
    component: FormControlsLab,
  },
  {
    label: 'Input',
    component: InputLab,
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
