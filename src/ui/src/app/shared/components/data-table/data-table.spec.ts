import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatPaginator } from '@angular/material/paginator';
import { SortDirection } from '@angular/material/sort';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it } from 'vitest';

import { DataTable, DataTableState } from './data-table';
import { DataTableCell } from './data-table-cell';
import { DataTableColumn } from './data-table-column';

interface SampleRow {
  id: string;
  name: string;
  size: number;
  status: string;
}

// Insertion order is intentionally not name-sorted so a sort assertion is
// meaningful. Twelve rows exercise the default page size of 10.
const sampleOrder = [3, 1, 2, 5, 4, 6, 8, 7, 9, 11, 10, 12];

function makeRows(): SampleRow[] {
  return sampleOrder.map((n) => ({
    id: `r${String(n).padStart(2, '0')}`,
    name: `Item ${String(n).padStart(2, '0')}`,
    size: n,
    status: n % 2 === 0 ? 'Active' : 'Draft',
  }));
}

@Component({
  selector: 'app-data-table-host',
  imports: [DataTable, DataTableCell],
  template: `
    <app-data-table
      [data]="rows()"
      [columns]="columns"
      [caption]="caption"
      [pageSize]="pageSize"
      [selectable]="selectable()"
      [selectedRow]="selectedRow()"
      [showPaginator]="showPaginator()"
      (rowClick)="onRowClick($event)"
    >
      <ng-template appDataTableCell="actions" let-row>
        <button type="button" class="row-action">Act {{ row.name }}</button>
      </ng-template>
    </app-data-table>
  `,
})
class DataTableHost {
  readonly rows = signal<SampleRow[]>(makeRows());
  caption = 'Sample table';
  pageSize = 10;

  readonly selectable = signal(false);
  readonly selectedRow = signal<SampleRow | null>(null);
  readonly showPaginator = signal(true);
  readonly clicked: SampleRow[] = [];

  onRowClick(row: SampleRow): void {
    this.clicked.push(row);
  }

  readonly columns: DataTableColumn<SampleRow>[] = [
    { key: 'id', header: 'ID', value: (row) => row.id, cellClass: 'mono' },
    { key: 'name', header: 'Name', value: (row) => row.name, sortable: true },
    { key: 'size', header: 'Size', value: (row) => row.size, align: 'end' },
    { key: 'status', header: 'Status', value: (row) => row.status },
    { key: 'actions', header: 'Actions' },
  ];
}

function dataRows(fixture: ComponentFixture<unknown>): HTMLTableRowElement[] {
  const rows = Array.from(
    fixture.nativeElement.querySelectorAll('tr'),
  ) as HTMLTableRowElement[];
  return rows.filter((row) => row.querySelector('td') !== null);
}

function headerByText(
  fixture: ComponentFixture<unknown>,
  text: string,
): HTMLElement | undefined {
  const headers = Array.from(
    fixture.nativeElement.querySelectorAll('th'),
  ) as HTMLElement[];
  return headers.find((header) => header.textContent?.trim() === text);
}

describe('DataTable', () => {
  let fixture: ComponentFixture<DataTableHost>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(DataTableHost);
    fixture.detectChanges();
  });

  it('renders a header for each configured column', () => {
    const headers = Array.from(
      fixture.nativeElement.querySelectorAll('th'),
    ).map((header) => (header as HTMLElement).textContent?.trim());

    expect(headers).toEqual(['ID', 'Name', 'Size', 'Status', 'Actions']);
  });

  it('renders text cells from the column value accessor', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Item 03');
    expect(text).toContain('Active');
    expect(text).toContain('Draft');
  });

  it('renders the visually hidden caption as the accessible name', () => {
    const caption = fixture.nativeElement.querySelector('caption');

    expect(caption?.textContent?.trim()).toBe('Sample table');
  });

  it('renders a projected custom cell with the row as context', () => {
    const actions = Array.from(
      fixture.nativeElement.querySelectorAll('.row-action'),
    ) as HTMLElement[];

    // One action per rendered row, each receiving its own row context.
    expect(actions).toHaveLength(10);
    expect(actions.every((action) => action.textContent?.startsWith('Act Item'))).toBe(
      true,
    );
  });

  it('limits rendered rows to the page size', () => {
    expect(dataRows(fixture)).toHaveLength(10);

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Item 06');
    expect(text).not.toContain('Item 12');
  });

  it('exposes an interactive sort control only on sortable columns', () => {
    // A sortable header renders a focusable role="button" control; a
    // non-sortable column (disabled mat-sort-header) renders none. role is a
    // standard ARIA attribute, not a Material-internal class.
    expect(
      headerByText(fixture, 'Name')?.querySelector('[role="button"]'),
    ).not.toBeNull();
    expect(
      headerByText(fixture, 'Size')?.querySelector('[role="button"]'),
    ).toBeNull();
  });

  it('sorts rows client-side when a sortable header is activated', async () => {
    fixture.componentInstance.pageSize = 50;
    fixture.detectChanges();
    await fixture.whenStable();

    headerByText(fixture, 'Name')?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const text = fixture.nativeElement.textContent as string;
    expect(text.indexOf('Item 01')).toBeLessThan(text.indexOf('Item 02'));
    expect(text.indexOf('Item 02')).toBeLessThan(text.indexOf('Item 03'));
  });

  it('emits rowClick when a selectable row is activated', () => {
    fixture.componentInstance.selectable.set(true);
    fixture.detectChanges();

    dataRows(fixture)[0].dispatchEvent(new Event('click'));

    expect(fixture.componentInstance.clicked).toHaveLength(1);
    expect(fixture.componentInstance.clicked[0].id).toBe(
      fixture.componentInstance.rows()[0].id,
    );
  });

  it('does not emit rowClick when the table is not selectable', () => {
    dataRows(fixture)[0].dispatchEvent(new Event('click'));

    expect(fixture.componentInstance.clicked).toHaveLength(0);
  });

  it('marks the selected row with the selected class', () => {
    fixture.componentInstance.selectable.set(true);
    fixture.componentInstance.selectedRow.set(
      fixture.componentInstance.rows()[0],
    );
    fixture.detectChanges();

    expect(dataRows(fixture)[0].classList.contains('selected')).toBe(true);
    expect(dataRows(fixture)[1].classList.contains('selected')).toBe(false);
  });

  it('hides the paginator and renders all rows when showPaginator is false', () => {
    fixture.componentInstance.showPaginator.set(false);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('mat-paginator')).toBeNull();
    expect(dataRows(fixture)).toHaveLength(12);
  });
});

@Component({
  selector: 'app-data-table-server-host',
  imports: [DataTable],
  template: `
    <app-data-table
      [data]="rows()"
      [columns]="columns"
      mode="server"
      [totalCount]="totalCount()"
      [pageIndex]="pageIndex()"
      [pageSize]="pageSize()"
      [sortActive]="sortActive()"
      [sortDirection]="sortDirection()"
      (stateChange)="onStateChange($event)"
    ></app-data-table>
  `,
})
class DataTableServerHost {
  // Twelve supplied rows with a page size of 10 prove the server page renders
  // unsliced. The total count simulates a larger backend set behind the page.
  readonly rows = signal<SampleRow[]>(makeRows());
  readonly totalCount = signal(40);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);
  readonly sortActive = signal('name');
  readonly sortDirection = signal<SortDirection>('asc');

  readonly states: DataTableState[] = [];

  readonly columns: DataTableColumn<SampleRow>[] = [
    { key: 'id', header: 'ID', value: (row) => row.id },
    { key: 'name', header: 'Name', value: (row) => row.name, sortable: true },
    {
      key: 'size',
      header: 'Size',
      value: (row) => row.size,
      sortable: true,
      align: 'end',
    },
    { key: 'status', header: 'Status', value: (row) => row.status },
  ];

  onStateChange(state: DataTableState): void {
    this.states.push(state);
  }
}

describe('DataTable server mode', () => {
  let fixture: ComponentFixture<DataTableServerHost>;
  let host: DataTableServerHost;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(DataTableServerHost);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders the supplied page unsliced regardless of page size', () => {
    // Client mode would slice to the page size of 10; server mode renders all
    // supplied rows because the data source is not paginated.
    expect(dataRows(fixture)).toHaveLength(12);
  });

  it('reflects the total count in the paginator range, not the row count', () => {
    const paginator = fixture.nativeElement.querySelector(
      'mat-paginator',
    ) as HTMLElement;

    expect(paginator.textContent).toContain('of 40');
  });

  it('emits stateChange with the new column on a sort column change', () => {
    headerByText(fixture, 'Size')?.click();
    fixture.detectChanges();

    expect(host.states).toHaveLength(1);
    expect(host.states[0]).toEqual({
      pageIndex: 0,
      pageSize: 10,
      sortActive: 'size',
      sortDirection: 'asc',
    });
  });

  it('emits stateChange with the toggled direction on a sort direction change', () => {
    // Name starts active ascending; with clearing disabled the next click
    // toggles to descending rather than clearing the sort.
    headerByText(fixture, 'Name')?.click();
    fixture.detectChanges();

    expect(host.states).toHaveLength(1);
    expect(host.states[0]).toEqual({
      pageIndex: 0,
      pageSize: 10,
      sortActive: 'name',
      sortDirection: 'desc',
    });
  });

  it('emits stateChange with the new page index on a page change', () => {
    const nextButton = fixture.nativeElement.querySelector(
      'button[aria-label="Next page"]',
    ) as HTMLButtonElement;
    nextButton.click();
    fixture.detectChanges();

    expect(host.states).toHaveLength(1);
    expect(host.states[0].pageIndex).toBe(1);
    expect(host.states[0].pageSize).toBe(10);
    expect(host.states[0].sortActive).toBe('name');
  });

  it('emits stateChange with the new page size on a page size change', () => {
    const paginator = fixture.debugElement.query(By.directive(MatPaginator))
      .componentInstance as MatPaginator;
    paginator.pageSize = 25;
    paginator.page.emit({
      previousPageIndex: 0,
      pageIndex: 0,
      pageSize: 25,
      length: 40,
    });
    fixture.detectChanges();

    expect(host.states).toHaveLength(1);
    expect(host.states[0].pageSize).toBe(25);
    expect(host.states[0].pageIndex).toBe(0);
  });
});
