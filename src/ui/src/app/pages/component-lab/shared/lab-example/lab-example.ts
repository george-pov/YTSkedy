import { Component, input } from '@angular/core';

@Component({
  selector: 'app-lab-example',
  templateUrl: './lab-example.html',
  styleUrl: './lab-example.scss',
})
export class LabExample {
  readonly titleId = input.required<string>();
  readonly title = input.required<string>();
  readonly itemsLayout = input<string>();
}
