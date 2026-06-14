import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChildren,
  effect,
  input,
  TemplateRef,
  viewChild,
} from '@angular/core';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule, SortDirection } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';

import { DataTableCell, DataTableCellContext } from './data-table-cell';
import { DataTableColumn } from './data-table-column';

/**
 * Generic, reusable data table over Angular Material `MatTable`, `MatSort`, and
 * `MatPaginator`. Sorting and pagination run client-side on the supplied data.
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

  /** Text shown when there are no rows to display. */
  readonly emptyText = input('');

  protected readonly dataSource = new MatTableDataSource<T>([]);

  private readonly sort = viewChild(MatSort);
  private readonly paginator = viewChild(MatPaginator);
  private readonly cells = contentChildren(DataTableCell);

  protected readonly displayedColumns = computed(() =>
    this.columns().map((column) => column.key),
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

    effect(() => {
      this.dataSource.sort = this.sort() ?? null;
    });

    effect(() => {
      this.dataSource.paginator = this.paginator() ?? null;
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
