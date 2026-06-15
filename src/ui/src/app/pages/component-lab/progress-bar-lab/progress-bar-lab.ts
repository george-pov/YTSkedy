import { ChangeDetectionStrategy, Component, signal } from '@angular/core';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { Button } from 'src/app/shared/components/button/button';
import { ProgressBar } from 'src/app/shared/components/progress-bar/progress-bar';

@Component({
  selector: 'app-progress-bar-lab',
  imports: [Button, ProgressBar, LabExample, LabPage],
  templateUrl: './progress-bar-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProgressBarLab {
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);

  private timer: ReturnType<typeof setTimeout> | undefined;

  // Simulates a page data fetch so the indeterminate page-load bar is visible
  // for a moment before the loaded content replaces it.
  protected load(): void {
    clearTimeout(this.timer);
    this.loaded.set(false);
    this.loading.set(true);

    this.timer = setTimeout(() => {
      this.loading.set(false);
      this.loaded.set(true);
    }, 2000);
  }
}
