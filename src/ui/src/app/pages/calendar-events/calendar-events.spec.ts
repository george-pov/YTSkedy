import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter, Router, UrlTree } from '@angular/router';
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
  let navigations: Array<string | UrlTree>;

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
    expect(text).toContain('Scheduled Start');
    expect(text).not.toContain('Scheduled Start (UTC)');
    expect(text).toContain('Title');
    expect(text).toContain('Publication Status');
    expect(text).toContain(
      'Friday, July 31, 2026 7:30 AM - America/Vancouver',
    );
    expect(text).not.toContain('Time Zone');
    expect(text).toContain('Stream title 1');
    expect(text).not.toContain('Description for stream 1');
  });

  it.each([
    ['NotPublished', ''],
    ['PartiallyPublished', 'Partially Published'],
    ['FullyPublished', 'Fully Published'],
    ['Failed', 'Failed'],
  ] as const)(
    'renders %s as exact text in the third cell',
    async (publicationStatus, expectedLabel) => {
      service.list.mockReturnValue(
        of(pageOf([draftEvent(calendarEventId, { publicationStatus })])),
      );

      await createComponent();

      const thirdCell = dataRows()[0].cells.item(2);
      expect(thirdCell).not.toBeNull();
      expect(thirdCell?.textContent?.trim()).toBe(expectedLabel);
    },
  );

  it('renders Publication Status as a non-sortable third column', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)])));

    await createComponent();

    const headers = Array.from(
      fixture.nativeElement.querySelectorAll('th'),
    ) as HTMLTableCellElement[];
    expect(headers.map((header) => header.textContent?.trim())).toEqual([
      'Scheduled Start',
      'Title',
      'Publication Status',
    ]);
    expect(headers[2].querySelector('[role="button"]')).toBeNull();
  });

  it('does not render an Actions column for a single edit affordance', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)])));

    await createComponent();

    expect(fixture.nativeElement.textContent).not.toContain('Actions');
    expect(fixture.nativeElement.querySelector('.edit-button')).toBeNull();
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

  it('renders the display title as a link in a hover-highlighted clickable row', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)])));

    await createComponent();

    const titleLink = fixture.nativeElement.querySelector(
      '.event-title-link',
    ) as HTMLAnchorElement;
    expect(titleLink).not.toBeNull();
    expect(titleLink.textContent?.trim()).toBe('Stream title 1');
    expect(titleLink.getAttribute('href')).toBe(
      `/calendar-events/${calendarEventId}/edit`,
    );
    expect(dataRows()[0].classList.contains('highlight-on-hover')).toBe(true);
    expect(dataRows()[0].classList.contains('clickable')).toBe(true);
    expect(dataRows()[0].classList.contains('selectable')).toBe(false);
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

  it('navigates to the edit form when a non-link part of the row is clicked', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)])));

    await createComponent();

    dataRows()[0].dispatchEvent(new Event('click'));

    expect(navigations).toEqual([`/calendar-events/${calendarEventId}/edit`]);
  });

  it('keeps title link clicks from bubbling to the row click handler', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)])));

    await createComponent();

    const titleLink = fixture.nativeElement.querySelector(
      '.event-title-link',
    ) as HTMLAnchorElement;
    const click = new MouseEvent('click', {
      bubbles: true,
      cancelable: true,
      button: 0,
    });
    titleLink.dispatchEvent(click);

    expect(navigations).toHaveLength(1);
    expect(typeof navigations[0]).not.toBe('string');
  });

  it('falls back to scheduled start when a table state has no API sort field', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)], 40)));

    await createComponent();
    service.list.mockClear();

    emitTableState({
      pageIndex: 0,
      pageSize: 10,
      sortActive: 'unknown',
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

  it('keeps the title link available from provider-neutral list rows', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent(calendarEventId)])));

    await createComponent();

    const titleLink = fixture.nativeElement.querySelector(
      '.event-title-link',
    ) as HTMLAnchorElement;
    expect(titleLink).not.toBeNull();
    expect(titleLink.getAttribute('href')).toBe(
      `/calendar-events/${calendarEventId}/edit`,
    );
  });

  function emitTableState(state: DataTableState): void {
    const table = fixture.debugElement.query(By.directive(DataTable))
      .componentInstance as DataTable<CalendarEvent>;
    table.stateChange.emit(state);
    fixture.detectChanges();
  }

  function dataRows(): HTMLTableRowElement[] {
    const rows = Array.from(
      fixture.nativeElement.querySelectorAll('tr'),
    ) as HTMLTableRowElement[];
    return rows.filter((row) => row.querySelector('td') !== null);
  }

  function draftEvent(
    calendarEventId: string,
    overrides: Partial<CalendarEvent> = {},
  ): CalendarEvent {
    return {
      calendarEventId,
      start: {
        localDateTime: '2026-07-31T07:30:00',
        timeZoneId: 'America/Vancouver',
      },
      scheduledStartUtc: '2026-07-31T14:30:00+00:00',
      displayTitle: 'Stream title 1',
      publicationStatus: 'NotPublished',
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
    router.navigateByUrl = ((url: string | UrlTree) => {
      navigations.push(url);
      return Promise.resolve(true);
    }) as Router['navigateByUrl'];

    fixture = TestBed.createComponent(CalendarEvents);
    fixture.detectChanges();
  }
});
