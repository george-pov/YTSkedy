# UI Routes

The Angular application configures routes in:

```text
src/ui/src/app/app.routes.ts
```

Route pages render through the application layout component in:

```text
src/ui/src/app/layout/app-layout/
```

For authenticated visitors the layout header shows a user badge (monogram plus
name) whose menu offers Sign Out. The badge identity is derived in the browser
from the Microsoft Entra External ID ID token `name` (Display Name) claim that
MSAL holds on the active account; no backend call is made. The current
`SignUpSignIn` user flow returns Display Name, so a true first and last name
(`given_name` / `family_name`) would require adding those attributes to the
flow's returned claims before the badge can use them. When no usable name claim
is present the badge falls back to the email local-part, then to a neutral
placeholder.

## Current Routes

| Path | Auth | Behavior |
| --- | --- | --- |
| `/` | Public | Renders `Home` with a sign-in button. Auto-redirects signed-in visitors to `/calendar-events`. |
| `/calendar-events` | Protected | Renders `CalendarEvents` and loads the first page of all events sorted by scheduled start descending. The scheduled start is shown as the UTC instant (`scheduledStartUtc`). Unauthenticated access triggers an Entra External ID redirect via `AuthFacade.signIn(returnUrl)`. |
| `/calendar-events/new` | Protected | Renders `CalendarEventDetails`, a Signal Forms form (a typed model plus a `schema()` of validators) that creates an event via `POST /api/calendar-events` and returns to `/calendar-events` on success. Guarded by `authenticatedGuard`. |
| `/calendar-events/:calendarEventId/edit` | Protected | Renders `CalendarEventDetails` in edit mode. Loads the event via `GET /api/calendar-events/{calendarEventId}`, repopulates the form, and keeps the scheduled start read-only. Save sends `PUT /api/calendar-events/{calendarEventId}` with the descriptions and is enabled only while the API's `canUpdate` flag is `true`. A Delete action removes a loaded event the API marks deletable (`canDelete`: a `Draft`, or a future `Published` event) via `DELETE /api/calendar-events/{calendarEventId}` and returns to `/calendar-events`. Guarded by `authenticatedGuard`. |
| `/signed-out` | Public | Renders post-logout confirmation. Auto-redirects already-authenticated visitors to `/calendar-events`. |
| `/component-lab` | Public | Renders the minimal component lab page for manually demoing shared UI components. |
| `**` | Public | Redirects to `/`. |

The `CalendarEvents` page calls
`GET /api/calendar-events?page={page}&pageSize={pageSize}&sort={sort}&direction={direction}`
through the shared API service. It requests one server-side sorted page at a
time (the first page defaults to scheduled start descending) and drives the
shared `app-data-table` in server mode from the returned
`{ items, page, pageSize, totalCount, sort, direction }` envelope. Each row
shows a Publish action only when the API's `canPublish` flag is `true`, and an
Edit icon that is always enabled and opens the details/edit view where Save and
Delete enforce `canUpdate` and `canDelete`. The HTTP
client attaches an Entra External ID access token via the YTSkedy-owned
`AuthFacade` and bearer interceptor (see
[`development/end-to-end-testing.md`](development/end-to-end-testing.md) and
[`../architecture/integration-contracts.md`](../architecture/integration-contracts.md)).
Richer calendar navigation and scheduling workflow behavior remain required
before the route is product-complete.

The `CalendarEventDetails` page calls `POST /api/calendar-events` through the
same shared API service and bearer interceptor, then navigates back to
`/calendar-events` on success. The list re-fetches its current page on load, so
a newly created event appears according to the server sort order and the active
page.

In edit mode (`/calendar-events/:calendarEventId/edit`) the page reads the id
from the route, calls `GET /api/calendar-events/{calendarEventId}` through the
same shared API service, and patches the loaded local start, time zone, and
descriptions into the form. While loading it shows a progress bar; a failed load
shows an inline error. The scheduled start is read-only in edit mode (the id is
derived from it), so only the descriptions are editable. The form shows a
read-only "Scheduled start (UTC)" translation of the local start: in edit mode
it is the stored `scheduledStartUtc`; in create mode it is derived live from the
chosen local date, time, and zone. Save sends
`PUT /api/calendar-events/{calendarEventId}` with the descriptions and navigates
back to `/calendar-events` on success.

In edit mode the page also shows a Delete action whenever the loaded event's
API-computed `canDelete` flag is `true` (a `Draft`, or a future `Published`
event with a recorded broadcast id); it is hidden in create mode. The UI reads
`canDelete` and does not inspect `status`, scheduled start, or
`youTubeBroadcastId` itself. Delete calls
`DELETE /api/calendar-events/{calendarEventId}` immediately with no confirmation
prompt and, on success, shows the `Calendar event deleted.` notification and
navigates back to `/calendar-events`. A `404` is treated as the event already
being gone (`Calendar event no longer exists.` then navigation); a `409` keeps
the page open with `The event can no longer be deleted. Reload the page and try
again.`; a `502` keeps the page open with `The YouTube broadcast could not be
deleted. Try again later.`; other failures show generic delete copy. Save is
enabled only while `canUpdate` is `true`, and an update `409` keeps the page
open with `The event can no longer be updated. Reload the page and try again.`
Save and Delete are mutually exclusive while either is in flight.

## Route Protection

`/calendar-events` is guarded by the YTSkedy-owned `authenticatedGuard`
(in `src/ui/src/app/shared/auth/authenticated-guard.ts`). The guard:

- Consults `AuthFacade.isAuthenticated()`.
- Calls `AuthFacade.signIn(returnUrl)` when not authenticated, capturing
  the requested URL so a direct deep link returns to the same route after
  sign-in.
- Never imports `@azure/msal-angular`; consumers depend on the facade only
  so MSAL stays a swappable adapter.

## Route Ownership

- Route configuration belongs in `src/ui/src/app/app.routes.ts`.
- Route-level page components belong under `src/ui/src/app/pages/`.
- Reusable display and form components should move under
  `src/ui/src/app/shared/` when they are introduced.
- API response mapping should live in explicit client or service code, not in
  route configuration.
