import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  type Mock,
  vi,
} from 'vitest';

import {
  CalendarEvent,
  CalendarEventsService,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { CalendarEvents } from './calendar-events';

describe('CalendarEvents', () => {
  let fixture: ComponentFixture<CalendarEvents>;
  let service: {
    listByMonth: Mock<
      (year: number, month: number) => Observable<CalendarEvent[]>
    >;
  };

  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-06-05T12:00:00Z'));

    service = {
      listByMonth: vi.fn<
        (year: number, month: number) => Observable<CalendarEvent[]>
      >(),
    };
  });

  afterEach(() => {
    TestBed.resetTestingModule();
    vi.useRealTimers();
  });

  it('loads and renders calendar events in a table', async () => {
    service.listByMonth.mockReturnValue(
      of([
        {
          calendarEventId: '20260606T170000Z',
          start: {
            localDateTime: '2026-06-06T10:00:00',
            timeZoneId: 'America/Vancouver',
          },
          descriptions: [
            {
              language: 'en',
              title: 'English stream 1',
              description: 'Description for stream 1 in English',
            },
          ],
        },
      ]),
    );

    await createComponent();

    expect(service.listByMonth).toHaveBeenCalledWith(2026, 6);
    expect(fixture.nativeElement.querySelector('table')).not.toBeNull();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('20260606T170000Z');
    expect(text).toContain('2026-06-06T10:00:00');
    expect(text).toContain('America/Vancouver');
    expect(text).toContain(
      'en: English stream 1 - Description for stream 1 in English',
    );
  });

  it('renders an empty state when the API response has no calendar events', async () => {
    service.listByMonth.mockReturnValue(of([]));

    await createComponent();

    expect(fixture.nativeElement.textContent).toContain(
      'No calendar events found.',
    );
    expect(fixture.nativeElement.querySelector('table')).toBeNull();
  });

  it('renders an error when calendar events cannot be loaded', async () => {
    service.listByMonth.mockReturnValue(
      throwError(() => new Error('Request failed')),
    );

    await createComponent();

    expect(fixture.nativeElement.textContent).toContain(
      'Calendar events could not be loaded.',
    );
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('renders an authorization-specific message on 403', async () => {
    service.listByMonth.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 403,
            statusText: 'Forbidden',
            url: 'https://api.example.test/api/calendar-events',
          }),
      ),
    );

    await createComponent();

    expect(fixture.nativeElement.textContent).toContain(
      'You do not have permission to view calendar events.',
    );
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  async function createComponent(): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [CalendarEvents],
      providers: [
        {
          provide: CalendarEventsService,
          useValue: service,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CalendarEvents);
    fixture.detectChanges();
  }
});
