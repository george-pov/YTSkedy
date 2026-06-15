import {
  ChangeDetectionStrategy,
  Component,
  inject,
  type OnInit,
  signal,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';

import {
  CalendarEvent,
  CalendarEventListQuery,
  CalendarEventSortField,
  CalendarEventStart,
  CalendarEventsService,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { NotificationService } from 'src/app/shared/notifications/notification-service';
import { Alert } from 'src/app/shared/components/alert/alert';
import { Button } from "src/app/shared/components/button/button";
import {
  DataTable,
  DataTableState,
} from 'src/app/shared/components/data-table/data-table';
import { DataTableCell } from 'src/app/shared/components/data-table/data-table-cell';
import { DataTableColumn } from 'src/app/shared/components/data-table/data-table-column';
import { Router } from '@angular/router';

@Component({
  selector: 'app-calendar-events',
  imports: [Alert, Button, DataTable, DataTableCell],
  templateUrl: './calendar-events.html',
  styleUrl: './calendar-events.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarEvents implements OnInit {

  private readonly router = inject(Router);
  private readonly calendarEventsService = inject(CalendarEventsService);
  private readonly notifications = inject(NotificationService);

  protected readonly events = signal<CalendarEvent[]>([]);
  // Single error surface for the page. Both a failed page load and a failed
  // publish set this; the template renders it in one place above the table.
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly publishingId = signal<string | null>(null);

  // Server-side paging and sorting state. `sortActive` is the table column key;
  // it is mapped to the API sort field when a request is built. The defaults
  // mirror the API defaults: the first page sorted by scheduled start
  // descending.
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);
  protected readonly sortActive = signal('start');
  protected readonly sortDirection =
    signal<DataTableState['sortDirection']>('desc');

  protected readonly columns: DataTableColumn<CalendarEvent>[] = [
    {
      key: 'start',
      header: 'Scheduled Start',
      value: (event) => formatScheduledStart(event.start.localDateTime),
      sortable: true,
    },
    {
      key: 'timeZone',
      header: 'Time Zone',
      value: (event) => event.start.timeZoneId,
      sortable: true,
    },
    {
      key: 'title',
      header: 'Title',
      value: (event) => englishTitle(event),
      sortable: true,
      truncate: true,
    },
    {
      key: 'status',
      header: 'Status',
      value: (event) => event.status,
      sortable: true,
    },
    {
      key: 'actions',
      header: 'Actions',
    },
  ];

  ngOnInit(): void {
    this.fetchPage();
  }

  protected addNewEvent() {
    this.router.navigateByUrl('/calendar-events/new');
  }

  protected onTableStateChange(state: DataTableState): void {
    this.sortActive.set(state.sortActive);
    this.sortDirection.set(state.sortDirection);

    // A page-size change invalidates the requested page index, so restart at
    // the first page; otherwise honor the requested page.
    const pageSizeChanged = state.pageSize !== this.pageSize();
    this.pageIndex.set(pageSizeChanged ? 0 : state.pageIndex);
    this.pageSize.set(state.pageSize);

    this.fetchPage();
  }

  protected canPublish(event: CalendarEvent): boolean {
    return event.status === 'Draft' && isFutureEvent(event.start);
  }

  protected publish(event: CalendarEvent): void {
    if (this.publishingId() !== null) {
      return;
    }

    this.errorMessage.set(null);
    this.publishingId.set(event.calendarEventId);

    this.calendarEventsService
      .publish(event.calendarEventId)
      .pipe(finalize(() => this.publishingId.set(null)))
      .subscribe({
        next: () => {
          // Re-fetch the current page so the published row keeps its place in
          // the server ordering instead of being patched in place.
          this.fetchPage();
          this.notifications.showSuccess('Calendar event published.');
        },
        error: (error: unknown) => {
          this.errorMessage.set(describePublishError(error));
        },
      });
  }

  private fetchPage(): void {
    this.isLoading.set(true);
    // Clear any prior error (from an earlier load or a failed publish) so a
    // stale message cannot linger above a freshly fetched page.
    this.errorMessage.set(null);

    const query: CalendarEventListQuery = {
      page: this.pageIndex(),
      pageSize: this.pageSize(),
      sort: toSortField(this.sortActive()),
      direction: this.sortDirection() === 'asc' ? 'asc' : 'desc',
    };

    this.calendarEventsService
      .list(query)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (page) => {
          this.events.set(page.items);
          this.totalCount.set(page.totalCount);
        },
        error: (error: unknown) => {
          this.events.set([]);
          this.totalCount.set(0);
          this.errorMessage.set(describeLoadError(error));
        },
      });
  }
}

// Presentation-only formatting of the wall-clock start as `YYYY-MM-DD HH:mm`,
// dropping the ISO `T` separator and the seconds. Sorting is server-side and
// unaffected by this; see `toSortField` and the data-table `server` mode.
// Falls back to the raw value if the expected shape is not present.
function formatScheduledStart(localDateTime: string): string {
  const match = /^(\d{4}-\d{2}-\d{2})T(\d{2}:\d{2})/.exec(localDateTime);

  return match === null ? localDateTime : `${match[1]} ${match[2]}`;
}

// Presentation-only English title for the list column. Returns the title of the
// English (`en`) localized description; both languages are required when an
// event is created, so a missing English entry renders as an empty cell rather
// than falling back to another language.
function englishTitle(event: CalendarEvent): string {
  const english = event.descriptions.find(
    (description) => description.language === 'en',
  );

  return english?.title ?? '';
}

// Maps a table column key to its API sort field. Only the sortable columns are
// mapped; any other key falls back to the scheduled-start default.
function toSortField(columnKey: string): CalendarEventSortField {
  switch (columnKey) {
    case 'status':
      return 'status';
    case 'timeZone':
      return 'timeZone';
    case 'title':
      return 'title';
    default:
      return 'scheduledStart';
  }
}

function describeLoadError(error: unknown): string {
  // 401 is handled centrally by the bearer-token interceptor (interactive
  // sign-in recovery). If we still surface a 401 to the page, it means
  // recovery is already in progress or has been skipped to break a loop;
  // a neutral message is appropriate. 403 indicates the signed-in user
  // lacks the required scope or app role and re-authenticating will not
  // change that, so call it out distinctly.
  if (error instanceof HttpErrorResponse && error.status === 403) {
    return 'You do not have permission to view calendar events.';
  }
  return 'Calendar events could not be loaded.';
}

function describePublishError(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 403) {
    return 'You do not have permission to publish calendar events.';
  }
  return 'Calendar event could not be published.';
}

function isFutureEvent(
  start: CalendarEventStart,
  now: Date = new Date(),
): boolean {
  const startValue = wallClockValue(start.localDateTime);
  const nowValue = currentWallClockValue(start.timeZoneId, now);

  // When the local date-time or time zone can't be interpreted, do not hide
  // the action; the backend still rejects past-dated publishes.
  if (startValue === null || nowValue === null) {
    return true;
  }

  return startValue > nowValue;
}

// Compares the event's wall-clock start against the current wall clock in the
// same time zone. Both sides reduce to comparable component values, so a
// daylight-saving boundary can only shift the result by the offset delta, which
// is immaterial for gating a button the backend re-validates.
function wallClockValue(localDateTime: string): number | null {
  const match =
    /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})$/.exec(localDateTime);

  if (match === null) {
    return null;
  }

  const [, year, month, day, hour, minute, second] = match;

  return Date.UTC(
    Number(year),
    Number(month) - 1,
    Number(day),
    Number(hour),
    Number(minute),
    Number(second),
  );
}

function currentWallClockValue(timeZoneId: string, now: Date): number | null {
  let parts: Intl.DateTimeFormatPart[];

  try {
    parts = new Intl.DateTimeFormat('en-US', {
      timeZone: timeZoneId,
      hourCycle: 'h23',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    }).formatToParts(now);
  } catch {
    return null;
  }

  const lookup = (type: Intl.DateTimeFormatPartTypes): number =>
    Number(parts.find((part) => part.type === type)?.value);

  return Date.UTC(
    lookup('year'),
    lookup('month') - 1,
    lookup('day'),
    lookup('hour'),
    lookup('minute'),
    lookup('second'),
  );
}
