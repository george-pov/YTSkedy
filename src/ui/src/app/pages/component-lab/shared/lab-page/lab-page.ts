import { Component, input } from '@angular/core';

@Component({
  selector: 'app-lab-page',
  templateUrl: './lab-page.html',
})
export class LabPage {
  readonly titleId = input.required<string>();
  readonly title = input.required<string>();
}
