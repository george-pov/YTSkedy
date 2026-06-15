import { Type } from '@angular/core';

import { AlertLab } from './alert-lab/alert-lab';
import { ButtonLab } from './button-lab/button-lab';
import { DataTableLab } from './data-table-lab/data-table-lab';
import { FormControlsLab } from './form-controls-lab/form-controls-lab';
import { ToolbarLab } from './toolbar-lab/toolbar-lab';

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
    label: 'Button',
    component: ButtonLab,
  },
  {
    label: 'Form Controls',
    component: FormControlsLab,
  },
  {
    label: 'Data Table',
    component: DataTableLab,
  },
  {
    label: 'Alert',
    component: AlertLab,
  },
];
