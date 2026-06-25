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
    expect(text).toContain('Scheduled Start (UTC)');
    expect(text).toContain('Title');
    expect(text).toContain('Status');
    expect(text).toContain('2026-06-06 17:00');
    expect(text).not.toContain('Time Zone');
    expect(text).toContain('English stream 1');
    expect(text).not.toContain('Description for stream 1 in English');
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
    service.list.mockReturnValue(of(pageOf([draftEvent('20260606T170000Z')])));

    await createComponent();

    const editButton = fixture.nativeElement.querySelector('.edit-button');
    expect(editButton).not.toBeNull();

    editButton.dispatchEvent(new Event('click'));

    expect(navigations).toEqual(['/calendar-events/20260606T170000Z/edit']);
  });

  it('re-fetches with the mapped sort field and direction on a table state change', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent('20260606T170000Z')], 40)));

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

  it('maps the Title column to the title sort field', async () => {
    service.list.mockReturnValue(of(pageOf([draftEvent('20260606T170000Z')], 40)));

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
    service.list.mockReturnValue(of(pageOf([draftEvent('20260606T170000Z')], 40)));

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
    service.list.mockReturnValue(of(pageOf([draftEvent('20260606T170000Z')], 40)));

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
    service.list.mockReturnValue(of(pageOf([draftEvent('20260606T170000Z')])));

    await createComponent();

    expect(fixture.nativeElement.querySelector('.publish-button')).toBeNull();
  });

  it('keeps the Edit icon enabled and navigable even when the row is not updatable', async () => {
    // Edit always opens the details/edit view; update eligibility is enforced on
    // Save (and Delete) inside that view, not on the list Edit icon.
    service.list.mockReturnValue(
      of(pageOf([{ ...draftEvent('20260606T170000Z'), canUpdate: false }])),
    );

    await createComponent();

    const editButton = fixture.nativeElement.querySelector(
      '.edit-button button',
    ) as HTMLButtonElement;
    expect(editButton).not.toBeNull();
    expect(editButton.disabled).toBe(false);

    fixture.nativeElement.querySelector('.edit-button').dispatchEvent(new Event('click'));

    expect(navigations).toEqual(['/calendar-events/20260606T170000Z/edit']);
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
    scheduledStartUtc = '2026-06-06T17:00:00+00:00',
  ): CalendarEvent {
    return {
      calendarEventId,
      start: {
        localDateTime,
        timeZoneId: 'America/Vancouver',
      },
      scheduledStartUtc,
      descriptions: [
        {
          language: 'en',
          title: 'English stream 1',
          description: 'Description for stream 1 in English',
        },
      ],
      status: 'Draft',
      canPublish: true,
      canUpdate: true,
      canDelete: true,
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
