import { ChangeDetectionStrategy, Component, signal } from '@angular/core';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { Button } from 'src/app/shared/components/button/button';
import {
  DataTable,
  DataTableState,
} from 'src/app/shared/components/data-table/data-table';
import { DataTableCell } from 'src/app/shared/components/data-table/data-table-cell';
import { DataTableColumn } from 'src/app/shared/components/data-table/data-table-column';

interface DemoRow {
  id: string;
  name: string;
  size: number;
  status: string;
}

@Component({
  selector: 'app-data-table-lab',
  imports: [DataTable, DataTableCell, Button, LabExample, LabPage],
  templateUrl: './data-table-lab.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DataTableLab {
  protected readonly columns: DataTableColumn<DemoRow>[] = [
    { key: 'id', header: 'ID', value: (row) => row.id, cellClass: 'mono' },
    { key: 'name', header: 'Name', value: (row) => row.name, sortable: true },
    {
      key: 'size',
      header: 'Size (MB)',
      value: (row) => row.size,
      sortable: true,
      align: 'end',
    },
    { key: 'status', header: 'Status', value: (row) => row.status, sortable: true },
    { key: 'actions', header: 'Actions' },
  ];

  protected readonly rows: DemoRow[] = Array.from({ length: 12 }, (_, index) => {
    const n = index + 1;
    return {
      id: `asset-${String(n).padStart(3, '0')}`,
      name: `Recording ${String(13 - n).padStart(2, '0')}`,
      size: (n * 37) % 100,
      status: n % 3 === 0 ? 'Published' : 'Draft',
    };
  });

  // Server-side example. The source dataset stands in for a backend store; the
  // table never sees it directly. Each state change derives a single page from
  // it, mirroring how the calendar events page consumes the paged API.
  protected readonly serverColumns: DataTableColumn<DemoRow>[] = [
    { key: 'id', header: 'ID', value: (row) => row.id, cellClass: 'mono' },
    { key: 'name', header: 'Name', value: (row) => row.name, sortable: true },
    {
      key: 'size',
      header: 'Size (MB)',
      value: (row) => row.size,
      sortable: true,
      align: 'end',
    },
    { key: 'status', header: 'Status', value: (row) => row.status, sortable: true },
  ];

  private readonly serverSource: readonly DemoRow[] = Array.from(
    { length: 40 },
    (_, index) => {
      const n = index + 1;
      return {
        id: `asset-${String(n).padStart(3, '0')}`,
        // Names run counter to the id order so a name sort is observable.
        name: `Recording ${String(41 - n).padStart(2, '0')}`,
        size: (n * 37) % 100,
        status: n % 3 === 0 ? 'Published' : 'Draft',
      };
    },
  );

  protected readonly serverSortActive = signal('name');
  protected readonly serverSortDirection =
    signal<DataTableState['sortDirection']>('asc');
  protected readonly serverPageIndex = signal(0);
  protected readonly serverPageSize = signal(10);
  protected readonly serverTotalCount = signal(this.serverSource.length);
  protected readonly serverPageData = signal<DemoRow[]>(
    this.deriveServerPage(
      this.serverSortActive(),
      this.serverSortDirection(),
      this.serverPageIndex(),
      this.serverPageSize(),
    ),
  );

  protected onServerStateChange(state: DataTableState): void {
    // A page-size change invalidates the requested index, so restart at the
    // first page; otherwise honor the requested page.
    const pageSizeChanged = state.pageSize !== this.serverPageSize();
    const pageIndex = pageSizeChanged ? 0 : state.pageIndex;

    this.serverSortActive.set(state.sortActive);
    this.serverSortDirection.set(state.sortDirection);
    this.serverPageIndex.set(pageIndex);
    this.serverPageSize.set(state.pageSize);
    this.serverPageData.set(
      this.deriveServerPage(
        state.sortActive,
        state.sortDirection,
        pageIndex,
        state.pageSize,
      ),
    );
    this.serverTotalCount.set(this.serverSource.length);
  }

  private deriveServerPage(
    sortActive: string,
    sortDirection: DataTableState['sortDirection'],
    pageIndex: number,
    pageSize: number,
  ): DemoRow[] {
    const ordered = [...this.serverSource];

    if (sortDirection !== '') {
      const factor = sortDirection === 'desc' ? -1 : 1;
      ordered.sort(
        (left, right) =>
          compareValues(
            this.serverSortValue(left, sortActive),
            this.serverSortValue(right, sortActive),
          ) * factor,
      );
    }

    const start = pageIndex * pageSize;
    return ordered.slice(start, start + pageSize);
  }

  private serverSortValue(row: DemoRow, columnKey: string): string | number {
    const column = this.serverColumns.find((entry) => entry.key === columnKey);
    return column?.value?.(row) ?? '';
  }
}

function compareValues(left: string | number, right: string | number): number {
  if (typeof left === 'number' && typeof right === 'number') {
    return left - right;
  }

  return String(left).localeCompare(String(right));
}
