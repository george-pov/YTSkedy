# UI Routes

The Angular route configuration lives in `src/ui/src/app/app.routes.ts`. Routed
pages render through `src/ui/src/app/layout/app-layout/`.

## Ownership

- Source of truth: browser routes, route protection, page behavior, navigation,
  user-visible actions, and pending-change behavior.
- Update when: a route, guard, route-level workflow, action, or navigation
  outcome changes.
- Do not duplicate: HTTP request and response shapes owned by `docs/api/http/`.

## Route Summary

| Path | Access | Page |
| --- | --- | --- |
| `/` | Public | Home and sign-in entry point |
| `/calendar-events` | Protected | Calendar event list |
| `/calendar-events/new` | Protected | Calendar event create page |
| `/calendar-events/:calendarEventId/edit` | Protected | Calendar event details and publication actions |
| `/templates` | Protected | Template list and editor |
| `/platforms` | Protected | Platform list and editor |
| `/settings` | Protected | Event text field settings editor |
| `/signed-out` | Public | Post-logout confirmation |
| `/component-lab` | Public | Shared component demonstrations |
| `**` | Public | Redirect to `/` |

## Application Layout

Authenticated routes render inside `AppLayout`. The header shows a user badge
with a Sign Out menu. The browser derives its display name from the active Entra
External ID account and falls back to the email local-part, then a neutral
placeholder. The badge does not call the backend for profile data.

## Home And Authentication Routes

The home route shows the sign-in entry point and redirects authenticated users to
`/calendar-events`. `/signed-out` shows logout confirmation and also redirects
an already-authenticated user to `/calendar-events`.

Protected routes use `authenticatedGuard`. When authentication is required, the
guard calls `AuthFacade.signIn(returnUrl)` so a successful sign-in returns the
user to the requested deep link. Route code depends on the app-owned auth
facade, not directly on MSAL Angular.

## Calendar Event List

`/calendar-events` requests one server-paged, server-sorted page through the
[calendar events contract](../api/http/calendar-events.md). The initial page is
sorted by scheduled start descending. Page, page-size, and supported sort
changes trigger a refetch.

The table shows Scheduled Start, Title, and Publication Status in that order.
All three columns are sortable through the API. Scheduled Start displays the
submitted local date-time followed by its IANA time-zone id while server
ordering uses the corresponding UTC instant. The Title displays the backend
`displayTitle`. Publication Status maps the required API aggregate as follows:

- `NotPublished`: empty cell.
- `PartiallyPublished`: `Partially Published`.
- `FullyPublished`: `Fully Published`.
- `Failed`: `Failed`.

The status is informational and does not control navigation or actions.
Clicking either a row or its title link opens the edit route.

## Calendar Event Create

`/calendar-events/new` uses `CalendarEventDetails` in create mode. It loads the
current [event text field setting](../api/http/event-text-fields.md), renders
short and long text controls by field type, and creates through the
[calendar events contract](../api/http/calendar-events.md).

The page may select, preview, and clear one JPEG or PNG thumbnail before create.
After the event is created, it uploads the selected file through the
[thumbnail contract](../api/http/calendar-event-thumbnails.md). A failed upload
does not discard the created event; the page keeps the event and shows a
thumbnail-specific error. Successful create returns to `/calendar-events`.

## Calendar Event Edit

`/calendar-events/:calendarEventId/edit` loads the stored event details. It uses
the stored scheduled start and event text snapshot rather than reshaping the
event from the current settings.

Backend-computed `canUpdate`, `canDelete`, and `canUpdateThumbnail` flags control
event mutation actions. Save changes is disabled until the normalized scheduled
start or text request differs from the saved baseline. A successful save updates
that baseline, clears the save error, shows `Calendar event updated.`, and stays
on the edit route.

The page fetches protected thumbnail bytes through the API and renders an object
URL. It never uses the protected API route directly as an image `src`.
Thumbnail upload and delete follow `canUpdateThumbnail` and the
[thumbnail contract](../api/http/calendar-event-thumbnails.md).

The platform table shows Type, Name, Status, and Actions from the details
response. Actions use the backend row flags documented by the
[platform publications contract](../api/http/platform-publications.md):

- Preview shows rendered or snapshotted title and description on demand.
- Preview remains available with pending event edits and states that stored
  values are used.
- Publish and publication delete are blocked until pending event edits are saved
  or discarded.
- Successful publish or publication delete refreshes details before applying
  root event action flags and clears an open preview for that platform.
- If the provider mutation succeeds but the details refresh fails, the page
  reports the partial success and directs the operator to reload before taking
  another action.
- A failed provider thumbnail application remains a published row with a
  warning; the page does not add a retry action.

Delete is available only in edit mode and follows backend `canDelete`. Pending
form changes are resolved before the delete confirmation opens. Successful
delete shows `Calendar event deleted.` and returns to the list. An already
missing event shows `Calendar event no longer exists.` and also returns to the
list. Publication conflicts keep the page open and direct the user to remove
platform publications first.

Cancel returns to the list when there are no pending edits. Pending edits use
the page-owned `Discard unsaved event changes?` confirmation. The route also
uses `pendingChangesGuard` so other route exits apply the same page-owned dirty
state and copy.

Save, delete, cancel, preview, publish, publication delete, and thumbnail
actions are mutually disabled while conflicting mutations are active.

## Templates

`/templates` is a single-page list and editor backed by the
[templates contract](../api/http/templates.md). It preselects the first displayed
template when rows exist.

`Add Template` opens create mode with a selectable immutable-on-create type.
Selecting a row opens edit mode. Create actions are Cancel and Save template;
edit actions are Delete, Cancel, and Save changes.

Pending changes compare the normalized save request: name normalization applies
while content remains exact. Save is disabled when that request matches the
saved baseline. Route exit, row selection, Add Template, Cancel, and dirty
delete ask before discarding edits. Load, save, delete, and duplicate-name
errors remain inline.

## Platforms

`/platforms` is a single-page list and editor backed by the
[configured platforms contract](../api/http/platforms.md). It shows Type, Name,
and Reference key and preselects the first displayed platform when rows exist.

`Add Platform` creates a YouTube or WordPress platform. The editor requires
title and description templates for the selected provider type. Create actions
are Cancel and Save platform; edit actions are Delete, Cancel, and Save changes.

Pending changes compare the normalized platform request. Blank replacement
secret fields preserve stored values and do not count as changes. Backend
redacted display strings may appear in blank replacement inputs but are never
copied into save requests. A typed replacement is visible while focused and
masked again on blur.

WordPress settings include an ordered category selection. New WordPress
platforms send `categoryIds: []` and show save-first guidance because lookup
uses stored platform credentials. After the first save, the operator searches
existing categories through the protected
[platform contract](../api/http/platforms.md), selects or removes category
chips, and saves again. The form stores IDs only. Category names, search
results, paging, loading, and inline provider errors remain transient selector
state and do not affect normalized dirty comparison.

Saved IDs are resolved in pages when the editor opens. If lookup fails or a
category no longer exists, the selector keeps a `Category #{id}` fallback so
the ID remains visible and removable. Save and Cancel preserve or restore the
ordered ID array through the same normalized request baseline as other platform
settings. Category lookup never receives the WordPress username or Application
Password from browser code.

Save is disabled when the normalized request matches the saved baseline. Route
exit, row selection, Add Platform, Cancel, and dirty delete ask before
discarding edits. Load, save, delete, duplicate-name, and duplicate-reference-key
errors remain inline.

## Settings

`/settings` edits the
[event text field setting](../api/http/event-text-fields.md). It displays the
derived `fieldKey`, label, type, max length, and delete action. Add and delete
renumber local `textN` keys immediately.

Save changes is disabled until the normalized settings request differs from the
saved baseline. Label-only trimming does not count as a change; add, delete,
renumber, type, and max-length edits do. A successful save replaces local state
and the baseline with the backend-normalized response.

Cancel asks before discarding pending edits, restores the last loaded or saved
baseline after confirmation, and clears transient save errors. Route exit uses
the same page-owned dirty state through `pendingChangesGuard`.

## Component Lab

`/component-lab` is a public manual demonstration surface for shared UI
components. It is not a production workflow or an alternative owner for shared
component documentation.

The lab includes interactive Basic and Disabled examples for the shared
`app-chip-list`. That component owns Material chip and autocomplete
presentation, controlled generic string-valued items, and accessible selection
and removal interactions. Provider lookup, paging, loading, errors, and numeric
category-ID mapping remain outside the shared component.

## Route Protection And Ownership

`/calendar-events`, `/templates`, `/platforms`, and `/settings` use
`authenticatedGuard`. The calendar event edit, Templates, Platforms, and
Settings routes also use `pendingChangesGuard`.

Route configuration belongs in `app.routes.ts`. Route-level page components
belong under `pages/`. Reusable presentation and form components belong under
`shared/`. Typed API mapping belongs in explicit client services, not route
configuration.
