import { Directive, inject, input, TemplateRef } from '@angular/core';

/** Render context passed to a projected `appDataTableCell` template. */
export interface DataTableCellContext<T> {
  readonly $implicit: T;
  readonly row: T;
  readonly index: number;
}

/**
 * Marks an `<ng-template>` as the custom cell renderer for a data-table column.
 *
 * The directive captures the template and the target column key. The
 * {@link DataTable} component collects these from projected content, keys them
 * by column, and renders the match in the matching body cell with the row as
 * `$implicit`.
 *
 * Usage:
 * ```html
 * <ng-template appDataTableCell="actions" let-row>...</ng-template>
 * ```
 */
@Directive({
  selector: '[appDataTableCell]',
})
export class DataTableCell<T = unknown> {
  /** Target column key. Matches a {@link DataTableColumn.key}. */
  readonly column = input.required<string>({ alias: 'appDataTableCell' });

  readonly template: TemplateRef<DataTableCellContext<T>> = inject(TemplateRef);
}
