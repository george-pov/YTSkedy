import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { DataTable } from './data-table';
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

  readonly columns: DataTableColumn<SampleRow>[] = [
    { key: 'id', header: 'ID', value: (row) => row.id, cellClass: 'mono' },
    { key: 'name', header: 'Name', value: (row) => row.name, sortable: true },
    { key: 'size', header: 'Size', value: (row) => row.size, align: 'end' },
    { key: 'status', header: 'Status', value: (row) => row.status },
    { key: 'actions', header: 'Actions' },
  ];
}

function dataRows(fixture: ComponentFixture<DataTableHost>): HTMLTableRowElement[] {
  const rows = Array.from(
    fixture.nativeElement.querySelectorAll('tr'),
  ) as HTMLTableRowElement[];
  return rows.filter((row) => row.querySelector('td') !== null);
}

function headerByText(
  fixture: ComponentFixture<DataTableHost>,
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
});
