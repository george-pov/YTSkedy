import { ChangeDetectionStrategy, Component } from '@angular/core';

import { LabExample } from 'src/app/pages/component-lab/shared/lab-example/lab-example';
import { LabPage } from 'src/app/pages/component-lab/shared/lab-page/lab-page';
import { Button } from 'src/app/shared/components/button/button';
import { DataTable } from 'src/app/shared/components/data-table/data-table';
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
}
