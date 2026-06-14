# UI Architecture

The Angular frontend lives under `src/ui/`.

## Current State

- `src/ui/` contains an Angular workspace managed with npm.
- The frontend package is named `ytskedy-ui`.
- `package.json` declares `npm@11.13.0` as the frontend package manager.
- Angular packages currently use version `22.0.0` ranges.
- TypeScript currently uses `~6.0.2`.
- Angular Material and Angular CDK are available UI dependencies. Repeated or
  app-owned Material usage should be isolated behind shared components under
  `src/ui/src/app/shared/components/`.
- Styling uses SCSS.
- Routing is configured through Angular router.
- Routed pages render through the `AppLayout` route shell.
- Runtime API base URL configuration is loaded from
  `src/ui/public/config/app-config.json`.
- The `calendar-events` page route loads the current browser month through the
  calendar events API service and renders the result through the shared
  `app-data-table` component, with client-side sorting and pagination. Rows
  keep API order until the user sorts; Scheduled Start, Time Zone, and Status
  are sortable, and the Actions column projects the conditional Publish button.
- Calendar events API service code lives under
  `src/ui/src/app/shared/api/calendar-events/`.

## Source Layout

Current application source lives under:

```text
src/ui/src/app/
```

Current route and page files:

```text
src/ui/src/app/app.routes.ts
src/ui/src/app/layout/app-layout/
src/ui/src/app/pages/calendar-events/
src/ui/src/app/shared/api/calendar-events/
src/ui/src/app/shared/config/
src/ui/src/app/shared/components/button/
src/ui/src/app/shared/components/data-table/
src/ui/src/app/shared/components/toolbar/
```

Use the page-first structure in
[`architecture/application-structure.md`](architecture/application-structure.md)
as the UI grows. Keep route-level pages under `pages/`, reusable browser UI
under `shared/`, and persistent application chrome under `layout/`.

## Shared Components

App-owned Angular Material usage is isolated behind shared components under
`src/ui/src/app/shared/components/`. Pages compose these components and do not
import Angular Material directly for those concerns.

### Data Table

`app-data-table` (`DataTable<T>` in
`src/ui/src/app/shared/components/data-table/`) is a generic, reusable table
that wraps Angular Material `MatTable`, `MatSort`, and `MatPaginator`. Sorting
and pagination run client-side on the supplied rows, and all Material table
directives stay internal to the component.

Inputs:

- `data` (`readonly T[]`, default `[]`): rows to render.
- `columns` (required `readonly DataTableColumn<T>[]`): column configuration.
- `caption` (default `''`): accessible name rendered as a visually hidden
  `<caption>`.
- `pageSize` (default `10`) and `pageSizeOptions` (default `[10, 25, 50]`).
- `sortActive` (default `''`) and `sortDirection` (`SortDirection`, default
  `''`): optional initial sort; empty means rows keep their supplied order
  until the user sorts.
- `emptyText` (default `''`): optional empty-state text.

`DataTableColumn<T>` carries `key`, `header`, `sortable?`, `value?`,
`cellClass?`, `align?`, and `truncate?`. A text column renders `value(row)`; a
column is sortable only when `sortable: true`; `truncate: true` clamps the cell
to one line with an ellipsis and sets the native `title` to the full value for
hover. Pages supply custom cell content with the `appDataTableCell` directive,
matched to a column by `key` and rendered with the row as context.

## Responsibilities

Use the UI workspace for browser-facing presentation and interaction behavior:

- Routes, layouts, pages, components, forms, and browser state.
- Frontend API client code that calls the Azure Functions REST API.
- Client-side formatting and interaction validation that improves the user
  experience before the backend receives a request.
- Frontend tests for components, routes, services, and user-facing behavior.

Do not place backend scheduling rules, persistence behavior, OAuth token
storage, YouTube API calls, WordPress API calls, or Azure Table Storage access
in the frontend. If frontend code needs those capabilities, add or call a
backend API use case.

## API Access

Frontend API access must be isolated behind explicit services or client
modules instead of being spread through components.

The canonical backend HTTP contracts live in [`../api/http/`](../api/http/).
UI docs should link to those contracts rather than duplicating request and
response shapes.

Deploy-specific public settings should use the runtime configuration approach
defined in
[`architecture/runtime-configuration.md`](architecture/runtime-configuration.md).

Current calendar events API access lives in
`src/ui/src/app/shared/api/calendar-events/calendar-events-service.ts`.
