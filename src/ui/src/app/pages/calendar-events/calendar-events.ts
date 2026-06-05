import {
  ChangeDetectionStrategy,
  Component,
  inject,
  type OnInit,
  signal,
} from '@angular/core';
import { finalize } from 'rxjs';

import {
  CalendarEvent,
  CalendarEventsService,
} from 'src/app/shared/api/calendar-events/calendar-events-service';

@Component({
  selector: 'app-calendar-events',
  imports: [],
  templateUrl: './calendar-events.html',
  styleUrl: './calendar-events.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarEvents implements OnInit {
  private readonly calendarEventsService = inject(CalendarEventsService);
  private readonly monthQuery = getCurrentMonthQuery();

  protected readonly events = signal<CalendarEvent[]>([]);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly monthLabel = formatMonthLabel(this.monthQuery);

  ngOnInit(): void {
    this.calendarEventsService
      .listByMonth(this.monthQuery.year, this.monthQuery.month)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (events) => {
          this.errorMessage.set(null);
          this.events.set(events);
        },
        error: () => {
          this.events.set([]);
          this.errorMessage.set('Calendar events could not be loaded.');
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
