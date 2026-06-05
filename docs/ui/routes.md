# UI Routes

The Angular application configures routes in:

```text
src/ui/src/app/app.routes.ts
```

Route pages render through the application layout component in:

```text
src/ui/src/app/layout/app-layout/
```

## Current Routes

| Path | Behavior |
| --- | --- |
| `/` | Renders `CalendarEvents` and loads the current browser month. |
| `/calendar-events` | Renders the same `CalendarEvents` page behavior as `/`. |
| `/component-lab` | Renders the minimal component lab page for manually demoing shared UI components. |
| `**` | Redirects to `/`. |

The current `CalendarEvents` page calls
`GET /api/calendar-events?year={year}&month={month}` through the shared API
service and renders a basic table. Richer calendar navigation and scheduling
workflow behavior remain required before the route is product-complete.

## Route Ownership

- Route configuration belongs in `src/ui/src/app/app.routes.ts`.
- Route-level page components belong under `src/ui/src/app/pages/`.
- Reusable display and form components should move under
  `src/ui/src/app/shared/` when they are introduced.
- API response mapping should live in explicit client or service code, not in
  route configuration.
