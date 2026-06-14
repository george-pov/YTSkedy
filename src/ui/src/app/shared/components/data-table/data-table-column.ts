/**
 * Column configuration for the shared {@link DataTable} component.
 *
 * A column is a text column when it provides {@link value} and no projected
 * cell template matches its {@link key}. A column is a custom column when a
 * page supplies a matching `appDataTableCell` template. A column may have both:
 * the projected template renders the cell while {@link value} still feeds
 * sorting.
 */
export interface DataTableColumn<T> {
  /** Unique column id. Also the match key for a projected cell template. */
  readonly key: string;

  /** Column header text. */
  readonly header: string;

  /** When true, the header is sortable. Defaults to false. */
  readonly sortable?: boolean;

  /**
   * Text and sort accessor. Required for a text column; optional for a column
   * rendered only by a projected template.
   */
  readonly value?: (row: T) => string | number;

  /**
   * Optional cell presentation class. Supported built-ins are `mono` and
   * `nowrap`, defined by the component stylesheet so encapsulation keeps them
   * reachable on the cell.
   */
  readonly cellClass?: string;

  /** Optional cell text alignment. Defaults to start. */
  readonly align?: 'start' | 'end';

  /**
   * When true, the cell is clamped to a single line with an ellipsis and the
   * native title is set to the full text value for hover.
   */
  readonly truncate?: boolean;
}
