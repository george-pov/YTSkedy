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
| `/` | Renders the `CalendarEvents` page component. |
| `/calendar-events` | Renders the `CalendarEvents` page component. |
| `**` | Redirects to `/`. |

The current `CalendarEvents` page is an initial route component. Loading data
from `GET /api/calendar-events?year={year}&month={month}` is required before
the calendar event workflow is product-complete.

## Route Ownership

- Route configuration belongs in `src/ui/src/app/app.routes.ts`.
- Route-level page components belong under `src/ui/src/app/pages/`.
- Reusable display and form components should move under
  `src/ui/src/app/shared/` when they are introduced.
- API response mapping should live in explicit client or service code, not in
  route configuration.
