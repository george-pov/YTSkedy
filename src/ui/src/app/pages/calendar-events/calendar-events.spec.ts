import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { By } from '@angular/platform-browser';
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
  CalendarEventListPage,
  CalendarEventListQuery,
  CalendarEventsService,
  PublishCalendarEventResponse,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import {
  DataTable,
  DataTableState,
} from 'src/app/shared/components/data-table/data-table';
import { CalendarEvents } from './calendar-events';

describe('CalendarEvents', () => {
  let fixture: ComponentFixture<CalendarEvents>;
  let service: {
    list: Mock<
      (query: CalendarEventListQuery) => Observable<CalendarEventListPage>
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
      list: vi.fn<
        (query: CalendarEventListQuery) => Observable<CalendarEventListPage>
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

  it('requests the first page sorted by scheduled start descending with no month scope', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent('20260606T170000Z')])));

    await createComponent();

    expect(service.list).toHaveBeenCalledTimes(1);
    expect(service.list).toHaveBeenCalledWith({
      page: 0,
      pageSize: 10,
      sort: 'scheduledStart',
      direction: 'desc',
    });
  });

  it('renders the returned page items in a table', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent('20260606T170000Z')])));

    await createComponent();

    expect(fixture.nativeElement.querySelector('table')).not.toBeNull();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Scheduled Start');
    expect(text).toContain('Time Zone');
    expect(text).toContain('Descriptions');
    expect(text).toContain('Status');
    expect(text).toContain('2026-06-06T10:00:00');
    expect(text).toContain('America/Vancouver');
    expect(text).toContain(
      'en: English stream 1 - Description for stream 1 in English',
    );
  });

  it('renders an empty state when the page has no items', async () => {
    service.list.mockReturnValue(of(pageOf([])));

    await createComponent();

    // The empty text is rendered by the data table's no-data row, so the table
    // is present even when there are no items.
    expect(fixture.nativeElement.textContent).toContain(
      'No calendar events found.',
    );
    expect(fixture.nativeElement.querySelector('table')).not.toBeNull();
  });

  it('renders an error when calendar events cannot be loaded', async () => {
    service.list.mockReturnValue(throwError(() => new Error('Request failed')));

    await createComponent();

    expect(fixture.nativeElement.textContent).toContain(
      'Calendar events could not be loaded.',
    );
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('renders an authorization-specific message on 403', async () => {
    service.list.mockReturnValue(
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
    service.list.mockReturnValue(of(pageOf([])));

    await createComponent();

    const addButton = fixture.nativeElement.querySelector('.add-new-button');
    expect(addButton).not.toBeNull();

    addButton.dispatchEvent(new Event('click'));

    expect(navigations).toEqual(['/calendar-events/new']);
  });

  it('re-fetches with the mapped sort field and direction on a table state change', async () => {
    service.list.mockReturnValue(
      of(pageOf([draftEvent('20260606T170000Z')], 40)),
    );

    await createComponent();
    service.list.mockClear();

    emitTableState({
      pageIndex: 0,
      pageSize: 10,
      sortActive: 'status',
      sortDirection: 'asc',
    });

    expect(service.list).toHaveBeenCalledTimes(1);
    expect(service.list).toHaveBeenCalledWith({
      page: 0,
      pageSize: 10,
      sort: 'status',
      direction: 'asc',
    });
  });

  it('re-fetches the requested page when the page index changes', async () => {
    service.list.mockReturnValue(
      of(pageOf([draftEvent('20260606T170000Z')], 40)),
    );

    await createComponent();
    service.list.mockClear();

    emitTableState({
      pageIndex: 2,
      pageSize: 10,
      sortActive: 'start',
      sortDirection: 'desc',
    });

    expect(service.list).toHaveBeenCalledWith({
      page: 2,
      pageSize: 10,
      sort: 'scheduledStart',
      direction: 'desc',
    });
  });

  it('resets to the first page when the page size changes', async () => {
    service.list.mockReturnValue(
      of(pageOf([draftEvent('20260606T170000Z')], 40)),
    );

    await createComponent();

    // Move to a later page first.
    emitTableState({
      pageIndex: 2,
      pageSize: 10,
      sortActive: 'start',
      sortDirection: 'desc',
    });
    service.list.mockClear();

    // A page-size change must restart at page 0 even though the emitted index
    // is still 2.
    emitTableState({
      pageIndex: 2,
      pageSize: 25,
      sortActive: 'start',
      sortDirection: 'desc',
    });

    expect(service.list).toHaveBeenCalledWith({
      page: 0,
      pageSize: 25,
      sort: 'scheduledStart',
      direction: 'desc',
    });
  });

  it('shows a Publish action for a future draft event', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent('20260606T170000Z')])));

    await createComponent();

    expect(
      fixture.nativeElement.querySelector('.publish-button'),
    ).not.toBeNull();
  });

  it('does not show a Publish action for a published event', async () => {
    service.list.mockReturnValue(
      of(pageOf([{ ...draftEvent('20260606T170000Z'), status: 'Published' }])),
    );

    await createComponent();

    expect(fixture.nativeElement.querySelector('.publish-button')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Published');
  });

  it('does not show a Publish action for a past draft event', async () => {
    service.list.mockReturnValue(
      of(pageOf([draftEvent('20260601T100000Z', '2026-06-01T10:00:00')])),
    );

    await createComponent();

    expect(fixture.nativeElement.querySelector('.publish-button')).toBeNull();
  });

  it('publishes a draft event and re-fetches the current page', async () => {
    const draft = draftEvent('20260606T170000Z');
    service.list
      .mockReturnValueOnce(of(pageOf([draft])))
      .mockReturnValueOnce(of(pageOf([{ ...draft, status: 'Published' }])));
    service.publish.mockReturnValue(
      of({
        calendarEventId: '20260606T170000Z',
        status: 'Published',
        youTubeBroadcastId: 'broadcast-123',
      }),
    );

    await createComponent();
    service.list.mockClear();

    fixture.nativeElement
      .querySelector('.publish-button')
      .dispatchEvent(new Event('click'));
    fixture.detectChanges();

    expect(service.publish).toHaveBeenCalledWith('20260606T170000Z');
    expect(service.list).toHaveBeenCalledTimes(1);
    expect(service.list).toHaveBeenCalledWith({
      page: 0,
      pageSize: 10,
      sort: 'scheduledStart',
      direction: 'desc',
    });
    expect(fixture.nativeElement.querySelector('.publish-button')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Published');
  });

  it('shows an error when publishing fails', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent('20260606T170000Z')])));
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

  function emitTableState(state: DataTableState): void {
    const table = fixture.debugElement.query(By.directive(DataTable))
      .componentInstance as DataTable<CalendarEvent>;
    table.stateChange.emit(state);
    fixture.detectChanges();
  }

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

  function pageOf(
    items: CalendarEvent[],
    totalCount = items.length,
  ): CalendarEventListPage {
    return {
      items,
      page: 0,
      pageSize: 10,
      totalCount,
      sort: 'scheduledStart',
      direction: 'desc',
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
