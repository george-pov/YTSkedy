# UI Architecture

The Angular frontend lives under `src/ui/`.

## Current State

- `src/ui/` contains an Angular workspace managed with npm.
- The frontend package is named `ytskedy-ui`.
- `package.json` declares `npm@11.13.0` as the frontend package manager.
- Angular packages currently use `22.0.x` ranges (`^22.0.2` for the framework,
  Material, and CDK; `^22.0.3` for the Angular CLI and build tooling).
- TypeScript currently uses `~6.0.2`.
- Angular Material and Angular CDK are available UI dependencies. Repeated or
  app-owned Material usage should be isolated behind shared components under
  `src/ui/src/app/shared/components/`.
- Styling uses SCSS.
- Routing is configured through Angular router.
- Routed pages render through the `AppLayout` route shell.
- Runtime API base URL configuration is loaded from
  `src/ui/public/config/app-config.json`.
- The `calendar-events` page route loads one server-side sorted page of events
  through the calendar events API service and renders the result through the
  shared `app-data-table` component in server mode. It defaults to the first
  page sorted by scheduled start descending, re-fetches on each sort, page, or
  page-size change. The Scheduled Start (UTC) and Title columns are sortable,
  and the Actions column projects the Edit button. The title display uses the
  backend `displayTitle` field, which is also the source for `title` sorting.
  The scheduled start is rendered as the UTC instant; local time and zone are
  shown on the create/edit form. Publishing is platform-scoped and is exposed
  from the calendar event details edit route.
- The `platforms` page route lists, creates, updates, and deletes configured
  publishing destinations through the platforms API service. It shows and edits
  each platform's optional Reference key, and exposes YouTube and WordPress
  provider settings. Title and description template selections are required
  publishing-content fields. WordPress Application Passwords are accepted on
  create and optional replacement updates, but existing passwords are never
  displayed in the browser.
- The `settings` page route reads, edits, renumbers, and saves the current
  event text fields list through the settings API service. Add and delete
  derive local `textN` keys immediately, and save replaces local state with the
  backend-normalized response.
- Calendar events API service code lives under
  `src/ui/src/app/shared/api/calendar-events/`.
- The calendar event details edit route consumes the single-event details
  response and renders its embedded platform publication rows through the
  shared `app-data-table` component as a Type, Name, Status, and Actions list.
  Create mode loads current event text fields from settings. Edit mode renders
  the event's stored `texts` snapshot and does not reshape it from the current
  setting. Rows with `canPublish: true` call the platform-scoped publish
  endpoint and update only that row from the publish response.
- Platforms API service code lives under
  `src/ui/src/app/shared/api/platforms/`.

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
src/ui/src/app/pages/calendar-event-details/
src/ui/src/app/pages/settings/
src/ui/src/app/pages/templates/
src/ui/src/app/pages/platforms/
src/ui/src/app/shared/api/calendar-events/
src/ui/src/app/shared/api/platforms/
src/ui/src/app/shared/api/settings/
src/ui/src/app/shared/api/templates/
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
that wraps Angular Material `MatTable`, `MatSort`, and `MatPaginator`. It
supports two modes through the `mode` input. In the default `client` mode,
sorting and pagination run client-side on the supplied rows. In `server` mode,
the component renders the supplied page as-is and emits state so the page can
fetch the matching server page. All Material table directives stay internal to
the component.

Inputs:

- `data` (`readonly T[]`, default `[]`): rows to render. In server mode this is
  the current page, rendered unsliced and unsorted by the client.
- `columns` (required `readonly DataTableColumn<T>[]`): column configuration.
- `caption` (default `''`): accessible name rendered as a visually hidden
  `<caption>`.
- `pageSize` (default `10`) and `pageSizeOptions` (default `[10, 25, 50]`).
- `sortActive` (default `''`) and `sortDirection` (`SortDirection`, default
  `''`): the active sort column key and direction. In client mode this is the
  optional initial sort; in server mode it reflects the server-applied sort.
- `mode` (`'client' | 'server'`, default `'client'`): paging and sorting mode.
- `totalCount` (default `0`): total row count across all server pages; drives
  the paginator length in server mode (client mode falls back to the data
  length).
- `pageIndex` (default `0`): active zero-based page index, bound to the
  paginator in server mode.
- `emptyText` (default `''`): optional empty-state text.

Output:

- `stateChange` (`DataTableState`): emitted in server mode on each page index,
  page size, sort column, or sort direction change. `DataTableState` is
  `{ pageIndex, pageSize, sortActive, sortDirection }`. Not emitted in client
  mode. In server mode the headers only toggle ascending/descending (sort
  clearing is disabled). The page maps the column key to its API sort field and
  fetches the matching page.

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
Templates API access lives in
`src/ui/src/app/shared/api/templates/templates-service.ts`.
Platforms API access lives in
`src/ui/src/app/shared/api/platforms/platforms-service.ts`.
Settings API access for event text fields lives in
`src/ui/src/app/shared/api/settings/event-text-fields-service.ts`.
