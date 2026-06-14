import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
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
  PublishCalendarEventResponse,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { CalendarEvents } from './calendar-events';

describe('CalendarEvents', () => {
  let fixture: ComponentFixture<CalendarEvents>;
  let service: {
    listByMonth: Mock<
      (year: number, month: number) => Observable<CalendarEvent[]>
    >;
    publish: Mock<
      (calendarEventId: string) => Observable<PublishCalendarEventResponse>
    >;
  };
  let navigations: string[];

  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-06-05T12:00:00Z'));

    navigations = [];
    service = {
      listByMonth: vi.fn<
        (year: number, month: number) => Observable<CalendarEvent[]>
      >(),
      publish: vi.fn<
        (calendarEventId: string) => Observable<PublishCalendarEventResponse>
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
          status: 'Draft',
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

  it('navigates to the create form when "Add new event" is clicked', async () => {
    service.listByMonth.mockReturnValue(of([]));

    await createComponent();

    const addButton = fixture.nativeElement.querySelector('.add-new-button');
    expect(addButton).not.toBeNull();

    addButton.dispatchEvent(new Event('click'));

    expect(navigations).toEqual(['/calendar-events/new']);
  });

  it('shows a Publish action for a future draft event', async () => {
    service.listByMonth.mockReturnValue(of([draftEvent('20260606T170000Z')]));

    await createComponent();

    expect(
      fixture.nativeElement.querySelector('.publish-button'),
    ).not.toBeNull();
  });

  it('does not show a Publish action for a published event', async () => {
    service.listByMonth.mockReturnValue(
      of([{ ...draftEvent('20260606T170000Z'), status: 'Published' }]),
    );

    await createComponent();

    expect(fixture.nativeElement.querySelector('.publish-button')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Published');
  });

  it('does not show a Publish action for a past draft event', async () => {
    service.listByMonth.mockReturnValue(
      of([draftEvent('20260601T100000Z', '2026-06-01T10:00:00')]),
    );

    await createComponent();

    expect(fixture.nativeElement.querySelector('.publish-button')).toBeNull();
  });

  it('publishes a draft event and marks it published', async () => {
    service.listByMonth.mockReturnValue(of([draftEvent('20260606T170000Z')]));
    service.publish.mockReturnValue(
      of({
        calendarEventId: '20260606T170000Z',
        status: 'Published',
        youTubeBroadcastId: 'broadcast-123',
      }),
    );

    await createComponent();

    fixture.nativeElement
      .querySelector('.publish-button')
      .dispatchEvent(new Event('click'));
    fixture.detectChanges();

    expect(service.publish).toHaveBeenCalledWith('20260606T170000Z');
    expect(fixture.nativeElement.querySelector('.publish-button')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Published');
  });

  it('shows an error when publishing fails', async () => {
    service.listByMonth.mockReturnValue(of([draftEvent('20260606T170000Z')]));
    service.publish.mockReturnValue(
      throwError(() => new Error('Request failed')),
    );

    await createComponent();

    fixture.nativeElement
      .querySelector('.publish-button')
      .dispatchEvent(new Event('click'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'Calendar event could not be published.',
    );
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  function draftEvent(
    calendarEventId: string,
    localDateTime = '2026-06-06T10:00:00',
  ): CalendarEvent {
    return {
      calendarEventId,
      start: {
        localDateTime,
        timeZoneId: 'America/Vancouver',
      },
      descriptions: [
        {
          language: 'en',
          title: 'English stream 1',
          description: 'Description for stream 1 in English',
        },
      ],
      status: 'Draft',
    };
  }

  async function createComponent(): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [CalendarEvents],
      providers: [
        provideRouter([]),
        {
          provide: CalendarEventsService,
          useValue: service,
        },
      ],
    }).compileComponents();

    const router = TestBed.inject(Router);
    router.navigateByUrl = ((url: string) => {
      navigations.push(url);
      return Promise.resolve(true);
    }) as Router['navigateByUrl'];

    fixture = TestBed.createComponent(CalendarEvents);
    fixture.detectChanges();
  }
});
