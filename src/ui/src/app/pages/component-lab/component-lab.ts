import { NgComponentOutlet } from '@angular/common';
import { Component } from '@angular/core';

import { componentLabItems } from './component-lab.registry';

@Component({
  selector: 'app-component-lab',
  imports: [NgComponentOutlet],
  templateUrl: './component-lab.html',
})
export class ComponentLab {
  protected readonly labItems = componentLabItems;
}
