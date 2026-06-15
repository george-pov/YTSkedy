import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChildren,
  effect,
  input,
  output,
  TemplateRef,
  viewChild,
} from '@angular/core';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule, SortDirection } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';

import { DataTableCell, DataTableCellContext } from './data-table-cell';
import { DataTableColumn } from './data-table-column';

/**
 * State emitted by {@link DataTable} in server mode whenever the active page
 * index, page size, sort column, or sort direction changes. The page maps the
 * sort column key to its API field and fetches the matching server page.
 */
export interface DataTableState {
  readonly pageIndex: number;
  readonly pageSize: number;
  readonly sortActive: string;
  readonly sortDirection: SortDirection;
}

/**
 * Generic, reusable data table over Angular Material `MatTable`, `MatSort`, and
 * `MatPaginator`. In the default `client` mode sorting and pagination run in
 * the browser on the supplied data. In `server` mode the supplied page renders
 * as-is and the component emits {@link stateChange} so the page can fetch the
 * matching server page; see {@link mode}.
 *
 * Columns are config-driven through {@link DataTableColumn}. Custom cell
 * content is supplied by the page through `appDataTableCell` templates; all
 * Material directives stay internal to this component.
 */
@Component({
  selector: 'app-data-table',
  imports: [NgTemplateOutlet, MatTableModule, MatSortModule, MatPaginatorModule],
  templateUrl: './data-table.html',
  styleUrl: './data-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DataTable<T> {
  readonly data = input<readonly T[]>([]);
  readonly columns = input.required<readonly DataTableColumn<T>[]>();

  /** Accessible name rendered as a visually hidden table caption. */
  readonly caption = input('');
  readonly pageSize = input(10);
  readonly pageSizeOptions = input<readonly number[]>([10, 25, 50]);

  /** Initial sort column key. Empty means no initial sort (data order). */
  readonly sortActive = input('');
  readonly sortDirection = input<SortDirection>('');

  /**
   * Paging and sorting mode. `client` (default) sorts and pages the supplied
   * data in the browser. `server` renders the supplied page as-is and emits
   * {@link stateChange} so the page can fetch the matching server page.
   */
  readonly mode = input<'client' | 'server'>('client');

  /**
   * Total row count across all server pages. Drives the paginator length in
   * server mode; client mode falls back to the supplied data length.
   */
  readonly totalCount = input(0);

  /** Active zero-based page index. Bound to the paginator in server mode. */
  readonly pageIndex = input(0);

  /** Text shown when there are no rows to display. */
  readonly emptyText = input('');

  /**
   * Emitted in server mode on each page index, page size, sort column, or sort
   * direction change. Not emitted in client mode.
   */
  readonly stateChange = output<DataTableState>();

  protected readonly dataSource = new MatTableDataSource<T>([]);

  private readonly sort = viewChild(MatSort);
  private readonly paginator = viewChild(MatPaginator);
  private readonly cells = contentChildren(DataTableCell);

  protected readonly displayedColumns = computed(() =>
    this.columns().map((column) => column.key),
  );

  protected readonly paginatorLength = computed(() =>
    this.mode() === 'server' ? this.totalCount() : this.data().length,
  );

  private readonly cellTemplates = computed(() => {
    const templates = new Map<string, TemplateRef<DataTableCellContext<T>>>();
    for (const cell of this.cells()) {
      templates.set(
        cell.column(),
        cell.template as TemplateRef<DataTableCellContext<T>>,
      );
    }
    return templates;
  });

  constructor() {
    this.dataSource.sortingDataAccessor = (row, columnId) => {
      const column = this.columns().find((entry) => entry.key === columnId);
      const value = column?.value?.(row);
      return value ?? '';
    };

    effect(() => {
      this.dataSource.data = [...this.data()];
    });

    // Client mode owns sorting and paging through the data source. Server mode
    // renders the supplied page as-is, so the sort and paginator stay
    // unattached and the data is neither sorted nor sliced in the browser.
    effect(() => {
      this.dataSource.sort =
        this.mode() === 'client' ? (this.sort() ?? null) : null;
    });

    effect(() => {
      this.dataSource.paginator =
        this.mode() === 'client' ? (this.paginator() ?? null) : null;
    });
  }

  protected onSortChange(): void {
    if (this.mode() === 'server') {
      this.emitState();
    }
  }

  protected onPageChange(): void {
    if (this.mode() === 'server') {
      this.emitState();
    }
  }

  private emitState(): void {
    const paginator = this.paginator();
    const sort = this.sort();
    this.stateChange.emit({
      pageIndex: paginator?.pageIndex ?? 0,
      pageSize: paginator?.pageSize ?? this.pageSize(),
      sortActive: sort?.active ?? '',
      sortDirection: sort?.direction ?? '',
    });
  }

  protected cellTemplate(
    key: string,
  ): TemplateRef<DataTableCellContext<T>> | null {
    return this.cellTemplates().get(key) ?? null;
  }

  protected cellClasses(column: DataTableColumn<T>): string {
    const classes: string[] = [];
    if (column.cellClass) {
      classes.push(column.cellClass);
    }
    if (column.align === 'end') {
      classes.push('align-end');
    }
    if (column.truncate) {
      classes.push('truncate');
    }
    return classes.join(' ');
  }

  protected cellText(column: DataTableColumn<T>, row: T): string {
    const value = column.value?.(row);
    return value === undefined || value === null ? '' : String(value);
  }
}
