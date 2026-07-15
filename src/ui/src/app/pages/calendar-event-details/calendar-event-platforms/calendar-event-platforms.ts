import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from 'src/app/shared/components/button/button';
import { DataTable } from 'src/app/shared/components/data-table/data-table';
import { DataTableCell } from 'src/app/shared/components/data-table/data-table-cell';
import { DataTableColumn } from 'src/app/shared/components/data-table/data-table-column';
import { type CalendarEventPlatform } from 'src/app/shared/api/calendar-events/calendar-events-service';
import {
  CalendarEventPlatformsState,
  platformStatusText,
  thumbnailStatusText,
} from './calendar-event-platforms.state';

@Component({
  selector: 'app-calendar-event-platforms',
  imports: [Alert, Button, DataTable, DataTableCell],
  templateUrl: './calendar-event-platforms.html',
  styleUrl: './calendar-event-platforms.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarEventPlatforms {
  readonly state = input.required<CalendarEventPlatformsState>();

  protected readonly platformColumns: readonly DataTableColumn<CalendarEventPlatform>[] = [
    { key: 'type', header: 'Type', value: (platform) => platform.platformType },
    { key: 'name', header: 'Name', value: (platform) => platform.platformName, truncate: true },
    { key: 'status', header: 'Status', value: platformStatusText },
    { key: 'actions', header: 'Actions' },
  ];
  protected readonly thumbnailStatusText = thumbnailStatusText;
  protected readonly platformStatusText = platformStatusText;
}
