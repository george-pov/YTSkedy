import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { By } from '@angular/platform-browser';
import { Observable, of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import {
  CalendarEvent,
  CalendarEventListPage,
  CalendarEventListQuery,
  CalendarEventsService,
} from 'src/app/shared/api/calendar-events/calendar-events-service';
import { DataTable, DataTableState } from 'src/app/shared/components/data-table/data-table';
import { CalendarEvents } from './calendar-events';

describe('CalendarEvents', () => {
  const calendarEventId = '6f9619ff8b864fb5bdfd4f5c2f2f16a1';

  let fixture: ComponentFixture<CalendarEvents>;
  let service: {
    list: Mock<(query: CalendarEventListQuery) => Observable<CalendarEventListPage>>;
  };
  let navigations: string[];

  beforeEach(() => {
    navigations = [];
    service = {
      list: vi.fn<(query: CalendarEventListQuery) => Observable<CalendarEventListPage>>(),
    };
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('requests the first page sorted by scheduled start descending with no month scope', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)])));

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
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)])));

    await createComponent();

    expect(fixture.nativeElement.querySelector('table')).not.toBeNull();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Scheduled Start (UTC)');
    expect(text).toContain('Title');
    expect(text).toContain('2026-06-06 17:00');
    expect(text).not.toContain('Time Zone');
    expect(text).toContain('Stream title 1');
    expect(text).not.toContain('Description for stream 1');
  });

  it('renders the display title returned by the API', async () => {
    service.list.mockReturnValue(
      of(
        pageOf([
          draftEvent(calendarEventId, {
            displayTitle: 'Backend display title',
            texts: [
              {
                fieldKey: 'text1',
                label: 'Description',
                type: 'LongText',
                maxLength: 2500,
                value: 'Text value that should not render',
              },
            ],
          }),
        ]),
      ),
    );

    await createComponent();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Backend display title');
    expect(text).not.toContain('Text value that should not render');
  });

  it('renders an empty state when the page has no items', async () => {
    service.list.mockReturnValue(of(pageOf([])));

    await createComponent();

    // The empty text is rendered by the data table's no-data row, so the table
    // is present even when there are no items.
    expect(fixture.nativeElement.textContent).toContain('No calendar events found.');
    expect(fixture.nativeElement.querySelector('table')).not.toBeNull();
  });

  it('renders an error when calendar events cannot be loaded', async () => {
    service.list.mockReturnValue(throwError(() => new Error('Request failed')));

    await createComponent();

    expect(fixture.nativeElement.textContent).toContain('Calendar events could not be loaded.');
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

  it('navigates to the edit form for the row when "Edit" is clicked', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)])));

    await createComponent();

    const editButton = fixture.nativeElement.querySelector('.edit-button');
    expect(editButton).not.toBeNull();

    editButton.dispatchEvent(new Event('click'));

    expect(navigations).toEqual([`/calendar-events/${calendarEventId}/edit`]);
  });

  it('falls back to scheduled start when a table state has no API sort field', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)], 40)));

    await createComponent();
    service.list.mockClear();

    emitTableState({
      pageIndex: 0,
      pageSize: 10,
      sortActive: 'actions',
      sortDirection: 'asc',
    });

    expect(service.list).toHaveBeenCalledTimes(1);
    expect(service.list).toHaveBeenCalledWith({
      page: 0,
      pageSize: 10,
      sort: 'scheduledStart',
      direction: 'asc',
    });
  });

  it('maps the Title column to the title sort field', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)], 40)));

    await createComponent();
    service.list.mockClear();

    emitTableState({
      pageIndex: 0,
      pageSize: 10,
      sortActive: 'title',
      sortDirection: 'asc',
    });

    expect(service.list).toHaveBeenCalledWith({
      page: 0,
      pageSize: 10,
      sort: 'title',
      direction: 'asc',
    });
  });

  it('re-fetches the requested page when the page index changes', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)], 40)));

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
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)], 40)));

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

  it('does not show a list-level Publish action because publishing is platform-scoped', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)])));

    await createComponent();

    expect(fixture.nativeElement.querySelector('.publish-button')).toBeNull();
  });

  it('keeps the Edit icon enabled and navigable from provider-neutral list rows', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)])));

    await createComponent();

    const editButton = fixture.nativeElement.querySelector(
      '.edit-button button',
    ) as HTMLButtonElement;
    expect(editButton).not.toBeNull();
    expect(editButton.disabled).toBe(false);

    fixture.nativeElement.querySelector('.edit-button').dispatchEvent(new Event('click'));

    expect(navigations).toEqual([`/calendar-events/${calendarEventId}/edit`]);
  });

  function emitTableState(state: DataTableState): void {
    const table = fixture.debugElement.query(By.directive(DataTable))
      .componentInstance as DataTable<CalendarEvent>;
    table.stateChange.emit(state);
    fixture.detectChanges();
  }

  function draftEvent(
    calendarEventId: string,
    overrides: Partial<CalendarEvent> = {},
  ): CalendarEvent {
    return {
      calendarEventId,
      start: {
        localDateTime: '2026-06-06T10:00:00',
        timeZoneId: 'America/Vancouver',
      },
      scheduledStartUtc: '2026-06-06T17:00:00+00:00',
      displayTitle: 'Stream title 1',
      texts: [
        {
          fieldKey: 'text1',
          label: 'Title',
          type: 'ShortText',
          maxLength: 50,
          value: 'Stream title 1',
        },
        {
          fieldKey: 'text2',
          label: 'Description',
          type: 'LongText',
          maxLength: 2500,
          value: 'Description for stream 1',
        },
      ],
      ...overrides,
    };
  }

  function pageOf(items: CalendarEvent[], totalCount = items.length): CalendarEventListPage {
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
