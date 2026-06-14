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
  CalendarEventStart,
  CalendarEventsService,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { Button } from "src/app/shared/components/button/button";
import { DataTable } from 'src/app/shared/components/data-table/data-table';
import { DataTableCell } from 'src/app/shared/components/data-table/data-table-cell';
import { DataTableColumn } from 'src/app/shared/components/data-table/data-table-column';
import { Router } from '@angular/router';

@Component({
  selector: 'app-calendar-events',
  imports: [Button, DataTable, DataTableCell],
  templateUrl: './calendar-events.html',
  styleUrl: './calendar-events.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarEvents implements OnInit {

  private readonly router = inject(Router);
  private readonly calendarEventsService = inject(CalendarEventsService);
  private readonly monthQuery = getCurrentMonthQuery();

  protected readonly events = signal<CalendarEvent[]>([]);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly publishingId = signal<string | null>(null);
  protected readonly publishError = signal<string | null>(null);
  protected readonly monthLabel = formatMonthLabel(this.monthQuery);

  protected readonly columns: DataTableColumn<CalendarEvent>[] = [
    {
      key: 'calendarEventId',
      header: 'Event ID',
      value: (event) => event.calendarEventId,
      cellClass: 'mono',
    },
    {
      key: 'start',
      header: 'Scheduled Start',
      value: (event) => event.start.localDateTime,
      sortable: true,
    },
    {
      key: 'timeZone',
      header: 'Time Zone',
      value: (event) => event.start.timeZoneId,
      sortable: true,
    },
    {
      key: 'descriptions',
      header: 'Descriptions',
      value: (event) => this.describeEvent(event),
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
    this.calendarEventsService
      .listByMonth(this.monthQuery.year, this.monthQuery.month)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (events) => {
          this.errorMessage.set(null);
          this.events.set(events);
        },
        error: (error: unknown) => {
          this.events.set([]);
          this.errorMessage.set(describeLoadError(error));
        },
      });
  }

  protected addNewEvent() {
    this.router.navigateByUrl('/calendar-events/new');
  }

  protected canPublish(event: CalendarEvent): boolean {
    return event.status === 'Draft' && isFutureEvent(event.start);
  }

  protected publish(event: CalendarEvent): void {
    if (this.publishingId() !== null) {
      return;
    }

    this.publishError.set(null);
    this.publishingId.set(event.calendarEventId);

    this.calendarEventsService
      .publish(event.calendarEventId)
      .pipe(finalize(() => this.publishingId.set(null)))
      .subscribe({
        next: (response) => {
          this.events.update((events) =>
            events.map((current) =>
              current.calendarEventId === event.calendarEventId
                ? { ...current, status: response.status }
                : current,
            ),
          );
        },
        error: (error: unknown) => {
          this.publishError.set(describePublishError(error));
        },
      });
  }

  protected describeEvent(event: CalendarEvent): string {
    if (event.descriptions.length === 0) {
      return 'No descriptions';
    }

    return event.descriptions
      .map((description) => {
        const summary = `${description.language}: ${description.title}`;

        return description.description === null
          ? summary
          : `${summary} - ${description.description}`;
      })
      .join('; ');
  }
}

interface CalendarMonthQuery {
  year: number;
  month: number;
}

function getCurrentMonthQuery(now: Date = new Date()): CalendarMonthQuery {
  return {
    year: now.getFullYear(),
    month: now.getMonth() + 1,
  };
}

function formatMonthLabel(query: CalendarMonthQuery): string {
  return new Intl.DateTimeFormat(undefined, {
    month: 'long',
    year: 'numeric',
  }).format(new Date(query.year, query.month - 1, 1));
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
