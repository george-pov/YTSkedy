import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { Toolbar } from 'src/app/shared/components/toolbar/toolbar';

@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, Toolbar],
  templateUrl: './app-layout.html',
  styleUrl: './app-layout.scss',
})
export class AppLayout {}
