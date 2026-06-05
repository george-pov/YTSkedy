import { Type } from '@angular/core';

import { ButtonLab } from './button-lab/button-lab';
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
];
