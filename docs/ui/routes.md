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
| `/calendar-events` | Protected | Renders `CalendarEvents` and loads one server-side sorted page of events through `GET /api/calendar-events`. The scheduled start is shown as the UTC instant (`scheduledStartUtc`), and the Title column displays the backend `displayTitle` field that also drives `title` sorting. Unauthenticated access triggers an Entra External ID redirect via `AuthFacade.signIn(returnUrl)`. |
| `/calendar-events/new` | Protected | Renders `CalendarEventDetails` in create mode. Loads current event text fields with `GET /api/settings/event-text-fields`, renders one control per configured field, creates via `POST /api/calendar-events`, and returns to `/calendar-events` on success. Guarded by `authenticatedGuard`. |
| `/calendar-events/:calendarEventId/edit` | Protected | Renders `CalendarEventDetails` in edit mode. Loads the event via `GET /api/calendar-events/{calendarEventId}`, renders the stored `texts` snapshot, keeps the scheduled start read-only, and shows the response `platforms` array as a Type, Name, Status, and Actions table. Platform preview, publish, and publication-delete actions call the platform-scoped endpoints and replace only the affected platform row. Save sends `PUT /api/calendar-events/{calendarEventId}` with text values. A separate Delete action calls `DELETE /api/calendar-events/{calendarEventId}` and returns to `/calendar-events` on success. Guarded by `authenticatedGuard`. |
| `/templates` | Protected | Renders `Templates`, a single-page CRUD for reusable social-post templates backed by the `templates` API through a typed `TemplatesService`. On load it lists templates with `GET /api/templates` and shows each template's type (platform) and name. New Template opens an unsaved editor whose type is selectable and creates via `POST /api/templates`. Selecting a row opens the editor with the type read-only (immutable after create) and saves name and content via `PUT /api/templates/{type}/{id}`; Delete calls `DELETE /api/templates/{type}/{id}`. A failed load, save, or delete shows an inline error, and a duplicate name surfaces the `409` conflict. Guarded by `authenticatedGuard`. |
| `/platforms` | Protected | Renders `Platforms`, a single-page CRUD for configured publishing destinations backed by the `platforms` API through a typed `PlatformsService`. On load it lists platforms with `GET /api/platforms` and shows Type, Name, and Reference key; New Platform creates a YouTube or WordPress platform via `POST /api/platforms`; selecting a row opens an editor that saves via `PUT /api/platforms/{platformId}` or deletes via `DELETE /api/platforms/{platformId}`. The editor requires title-template and description-template selections backed by `GET /api/templates?type={type}`. In edit mode, backend-provided redacted secret display strings appear inside the blank replacement inputs, hide while the input is focused, and return on blur when no replacement is entered; blank saves preserve stored secrets. A failed load, save, or delete shows an inline error, and duplicate names or duplicate reference keys surface the `409` conflict. Guarded by `authenticatedGuard`. |
| `/settings` | Protected | Renders `Settings`, an event text field editor backed by `GET /api/settings/event-text-fields` and `PUT /api/settings/event-text-fields`. The page shows the derived `fieldKey`, label, type, max length, and delete action for each field; add and delete renumber local `textN` keys immediately, and save replaces local state with the backend-normalized response. Guarded by `authenticatedGuard`. |
| `/signed-out` | Public | Renders post-logout confirmation. Auto-redirects already-authenticated visitors to `/calendar-events`. |
| `/component-lab` | Public | Renders the minimal component lab page for manually demoing shared UI components. |
| `**` | Public | Redirects to `/`. |

The `CalendarEvents` page calls
`GET /api/calendar-events?page={page}&pageSize={pageSize}&sort={sort}&direction={direction}`
through the shared API service. It requests one server-side sorted page at a
time (the first page defaults to scheduled start descending) and drives the
shared `app-data-table` in server mode from the returned
`{ items, page, pageSize, totalCount, sort, direction }` envelope. Each row
shows an Edit icon that opens the details/edit view where Save, Delete, and
platform-scoped Publish and Delete publication actions enforce the
API-computed action flags. The HTTP client attaches an Entra External ID access
token via the YTSkedy-owned `AuthFacade` and bearer interceptor (see
[`development/end-to-end-testing.md`](development/end-to-end-testing.md) and
[`../architecture/integration-contracts.md`](../architecture/integration-contracts.md)).
Richer calendar navigation and scheduling workflow behavior remain required
before the route is product-complete.

The `CalendarEventDetails` page calls `POST /api/calendar-events` through the
same shared API service and bearer interceptor, then navigates back to
`/calendar-events` on success. In create mode it first loads the current event
text field list from `EventTextFieldsService`. `ShortText` fields render as
single-line inputs and `LongText` fields render as multiline inputs. The list
re-fetches its current page on load, so a newly created event appears according
to the server sort order and the active page.

In edit mode (`/calendar-events/:calendarEventId/edit`) the page reads the id
from the route, calls `GET /api/calendar-events/{calendarEventId}` through the
same shared API service, and patches the loaded local start, time zone, stored
`texts` snapshot, and `platforms` array into page state. It does not call the
current settings endpoint to reshape an existing event. The `platforms` array
is rendered through `app-data-table`, showing platform type, name, and publish
status from the API response. Rows with `canPreviewPublishingContent: true`
show a Preview action that calls
`GET /api/calendar-events/{calendarEventId}/platforms/{platformId}/publishing-content`
and displays the returned `Preview` or `Snapshot` title and description below
the table. The preview surface is on demand and does not add title or
description columns to the table. Rows with `canPublish: true` show a Publish
action that calls
`POST /api/calendar-events/{calendarEventId}/platforms/{platformId}/publish`.
On success, the row is updated from the publish response and the Publish action
is removed for that row; any open preview for that platform is cleared. Rows
with `canDeletePublication: true` show an icon button with the accessible label
`Delete publication for {platformName}`. That action opens a confirmation
dialog and, after confirmation, calls
`DELETE /api/calendar-events/{calendarEventId}/platforms/{platformId}/publication`.
On success, only the affected platform row is replaced from the API response
and any open preview for that platform is cleared; the page does not reload the
event and does not overwrite unsaved text edits.

A preview `409` keeps the page open with
`Publishing content cannot be previewed. Reload the page and try again.` A
publication-delete `409` keeps the page open with
`The publication can no longer be deleted. Reload the page and try again.`; a
`502` keeps the page open with
`The provider publication could not be deleted. Try again later.`; other
failures show generic publication-delete copy. While loading it shows a
progress bar; a failed load shows an inline error. The scheduled start is
read-only in edit mode, so only the text values are editable. The form shows a
read-only "Scheduled start (UTC)" translation of the local start: in edit mode
it is the stored `scheduledStartUtc`; in create mode it is derived live from
the chosen local date, time, and zone. Save sends
`PUT /api/calendar-events/{calendarEventId}` with the text values and navigates
back to `/calendar-events` on success.

In edit mode the page also shows a Delete action; it is hidden in create mode.
Delete calls `DELETE /api/calendar-events/{calendarEventId}` immediately with
no confirmation prompt and, on success, shows the `Calendar event deleted.`
notification and navigates back to `/calendar-events`. A `404` is treated as
the event already being gone (`Calendar event no longer exists.` then
navigation); a `409` keeps the page open with
`Delete platform publications before deleting this event.`; other failures show
generic delete copy. An update `409` keeps the page open with
`The event can no longer be updated. Reload the page and try again.` Save and
Delete are mutually exclusive while either is in flight. Save, Delete, Cancel,
Publish, and Delete publication are disabled while a publishing-content
preview, platform publish, or platform-publication delete is in flight.

The `Platforms` page calls `GET /api/platforms` through the shared platforms
API service and maps the backend `{ items: [...] }` envelope plus `platformId`
field into the page-facing platform model. The table shows type, name, and the
optional Reference key. Create sends the selected type, name, reference key,
platform `publishingContent`, and provider-specific publish settings to
`POST /api/platforms`; the create type select offers YouTube and WordPress.
The title-template and description-template selectors list templates for the
selected platform type and require a selected template id. In create mode,
changing the platform type resets selected template ids that are not available
for the new type. YouTube settings include client ID, client secret, refresh
token, privacy status, and made-for-kids flag. WordPress settings include site
URL, username, Application Password, and post status. Edit sends name,
reference key, `publishingContent`, and publish settings to
`PUT /api/platforms/{platformId}`. The Reference key field preserves casing for
display; blank input sends `null` and clears the stored key. For YouTube, the
client secret and refresh token replacement inputs are intentionally blank on
edit. Backend-provided redacted display strings are shown inside those blank
inputs, hide while the input is focused, and return on blur when no replacement
value is entered. Blank values are omitted so the API preserves the stored
values. For WordPress, the Application Password replacement input is
intentionally blank on edit. The password display string appears inside that
blank input, hides on focus, returns on blur when left blank, and a non-blank
value replaces it. Redacted display values are not copied into create or update
requests. The exact API response fields are documented in
[`../api/http/platforms.md`](../api/http/platforms.md). Delete calls
`DELETE /api/platforms/{platformId}` and removes the row after a successful
`204 No Content`. The HTTP client attaches an Entra External ID access token
through the same bearer interceptor and calendar-event scopes used by the other
protected API resources.

The `Settings` page calls `GET /api/settings/event-text-fields` on load through
`EventTextFieldsService`. It renders the current ordered field list and keeps
the derived `fieldKey` read-only in the UI. Add appends a new field and derives
the next `textN` key immediately. Delete removes the row and renumbers later
fields immediately. Save sends the ordered fields to
`PUT /api/settings/event-text-fields`; the page replaces its local model with
the backend response so backend normalization is the final source of truth. A
failed load or save shows an inline error. The route is protected by the same
bearer interceptor and calendar-event scopes as the rest of the application API.

## Route Protection

`/calendar-events`, `/templates`, `/platforms`, and `/settings` are guarded by
the YTSkedy-owned `authenticatedGuard`
(in `src/ui/src/app/shared/auth/authenticated-guard.ts`). The guard:

- Consults `AuthFacade.isAuthenticated()`.
- Calls `AuthFacade.signIn(returnUrl)` when not authenticated, capturing the
  requested URL so a direct deep link returns to the same route after sign-in.
- Never imports `@azure/msal-angular`; consumers depend on the facade only so
  MSAL stays a swappable adapter.

## Route Ownership

- Route configuration belongs in `src/ui/src/app/app.routes.ts`.
- Route-level page components belong under `src/ui/src/app/pages/`.
- Reusable display and form components should move under
  `src/ui/src/app/shared/` when they are introduced.
- API response mapping should live in explicit client or service code, not in
  route configuration.
