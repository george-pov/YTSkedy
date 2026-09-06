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
| `/settings` | Protected | Calendar event defaults editor |
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
current event text fields from the
[calendar event defaults](../api/http/calendar-event-defaults.md), renders short
and long text controls by field type, and creates through the
[calendar events contract](../api/http/calendar-events.md).

Create mode independently requests a start suggestion. It sends the curated,
supported browser-detected time zone only as a fallback. Returned date, time,
and zone values initialize untouched start controls; null properties preserve
their existing values. A late response does not overwrite operator start input,
and edit mode never requests or applies a suggestion. Suggestion failure shows
nonfatal guidance near Scheduled start and leaves manual entry available.

The page may select, preview, and clear one JPEG or PNG thumbnail before create.
After the event is created, it uploads the selected file through the
[thumbnail contract](../api/http/calendar-event-thumbnails.md). A failed upload
does not discard the created event; the browser opens its edit route with a
thumbnail-specific error. Otherwise, successful create returns to
`/calendar-events`. Pending create state is cleared before either guarded
navigation.

## Calendar Event Edit

`/calendar-events/:calendarEventId/edit` loads the stored event details. It uses
the stored scheduled start and event text snapshot rather than reshaping the
event from the current settings.

Backend-computed `canUpdate`, `canDelete`, and `canUpdateThumbnail` flags control
event mutation actions. Save changes is disabled until the normalized scheduled
start or text request differs from the saved baseline. A successful save commits
the exact submitted draft as the new baseline, clears the save error, shows
`Calendar event updated.`, and stays on the edit route. Values entered while the
request is in flight remain visible and pending against that submitted baseline.

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
- Pending-change platform guidance clears whenever the normalized draft returns
  to its saved baseline through Cancel, a successful save, or manual editing.
  Publish, publication-delete, recovery, and preview errors retain their
  operation-specific lifecycle.
- Successful publish or publication delete refreshes details before applying
  root event action flags and clears an open preview for that platform.
- If the provider mutation succeeds but the details refresh fails, the page
  reports the partial success and directs the operator to reload before taking
  another action.
- A failed provider thumbnail application remains a published row with a
  warning; the page does not add a retry action.
- A publication row with status `Failed` shows Failed and keeps Publish enabled
  only when the backend returns `canPublish: true`. Publish first opens a
  warning that tells the operator to verify the event on the publishing
  platform and delete it there if necessary before retrying. A successful retry
  refreshes the full event details.
- An eligible stale `Publishing` row shows `Mark as failed` only when the
  backend returns `canRecoverPublication: true`. The warning requires provider
  verification and states that only the local attempt becomes `Failed`; it does
  not claim the provider resource is absent or delete it. Success refreshes the
  details. `404` and `409` responses direct the operator to reload.
- Publish errors use the structured provider failure when available. WordPress
  rate limits show retry timing, authentication and permission failures identify
  the settings to check, and every retained diagnostic shows the publish attempt
  reference for log correlation. Failed rows keep this guidance after reload.
  Unknown `502` errors show the existing operator verification guidance. A
  publication-delete response with code `publication_target_mismatch` shows
  `YTSkedy cannot delete this publication because the platform settings no
  longer match the target used to create it. Restore the original platform
  target and try again.` Other delete conflicts keep the generic reload
  guidance.

Normal route deactivation is denied while create, update, delete, thumbnail,
publish, publication-delete, or stale-recovery mutations are active. Preview,
initial load, and other reads do not block navigation and retain
destroyed-component cancellation. Successful create, delete, and
upload-after-create flows clear their mutation state before application-driven
navigation.

Calendar Event create and edit show an always-enabled
`Back to calendar events` parent-navigation link above the page title. A
decorative left arrow identifies the backward direction, and the link has an
explicit `/calendar-events` destination rather than depending on browser
history. Both routes use
`pendingChangesGuard`: clean transitions proceed, pending changes use the
page-owned discard confirmation, and active mutations block route deactivation.

Cancel is disabled while the normalized form is clean or a conflicting
mutation is active. When changes are pending, confirmed Cancel restores the
initialized or last-saved form baseline in place and does not navigate. A
thumbnail selected before create participates in pending state and is cleared
by confirmed Cancel. A thumbnail upload or delete completed for an existing
event remains committed and is not rolled back.

Delete is available only in edit mode and follows backend `canDelete`. Pending
form changes are resolved before the delete confirmation opens. Successful
delete shows `Calendar event deleted.` and returns to the list. An already
missing event shows `Calendar event no longer exists.` and also returns to the
list. Publication conflicts keep the page open and direct the user to remove
platform publications first.

Back and other route exits continue to their requested destination only after
route protection allows them. Cancel is a local reset action. Delete retains
its separate confirmation and navigation flow after pending edits are
resolved.

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
while content remains exact. Save and Cancel are disabled when that request
matches the current baseline or a save/delete mutation is active. Dirty Cancel
asks for confirmation and restores the current create or edit baseline in
place without closing the editor, changing mode, or clearing the selected row.
Route exit, row selection, and Add Template use the same page-owned discard
decision before continuing their target-changing action. Delete uses one
warning before permanently removing the template. When the editor is dirty,
that warning also names the unsaved template values that will be lost.
Canceling or dismissing the warning preserves the edits. A failed delete also
keeps the dirty editor available for retry or Save. Load, save, delete, and
duplicate-name errors remain inline.

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

YouTube settings include four page-owned single-select controls:

- Stream language begins with `YouTube Default`, which sends
  `defaultAudioLanguage: null`, followed by the application-owned catalog
  reviewed against YouTube Studio on 2026-07-16. It includes `zxx` as
  `Not applicable`.
- Title and description language begins with `YouTube Default`, which sends
  `defaultLanguage: null`. Its catalog has the same common codes and labels as
  Stream language but does not include `zxx`.
- Category begins with `YouTube Default`, which sends `categoryId: null`, then
  lists the application-owned US categories reviewed as assignable on
  2026-07-14. The list is static source data and never performs runtime YouTube
  or backend category lookup. An unknown stored id remains visible as
  `Category #{id}` until the operator changes it.
- Altered or synthetic content uses No and Yes and sends
  `containsSyntheticMedia: false` or `true`. Existing settings without the
  property and new forms default to No.

All four controls use the existing `app-select` single-value interaction. The
language catalogs are static and perform no runtime YouTube or backend lookup.
An unknown stored language remains visible as `Language code: {code}` and is
preserved across save, cancel, and reopen until the operator changes it.
YouTube does not use the WordPress category chip selector.

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

Save and Cancel are disabled when the normalized request matches the current
baseline or a save/delete mutation is active. Dirty Cancel asks for
confirmation and restores the provider form baseline in place. Edit mode and
the selected row remain unchanged; create mode remains open without selecting a
row. Replacement secret fields remain blank, redacted display values stay
display-only, and restored provider inputs may trigger only the existing
read-only template or category refresh. Route exit, row selection, and Add
Platform retain their target-changing behavior after the discard decision
resolves. Delete uses one warning that provider publications are not removed
and cannot be deleted through YTSkedy after the platform is removed. When the
editor is dirty, the same warning also names the unsaved platform values that
will be lost. Canceling or dismissing the warning preserves those edits. A
failed delete also keeps the dirty editor available for retry or Save. Load,
save, delete, duplicate-name, and duplicate-reference-key errors remain inline.

## Settings

`/settings` edits the combined
[calendar event defaults](../api/http/calendar-event-defaults.md).
The event text section displays the derived `fieldKey`, label, type, max length,
and delete action. Add and delete renumber local `textN` keys immediately. The
New calendar event defaults section independently manages optional weekday,
local time, and time zone values, including a No default state for each.

Save changes is disabled until either section differs from the normalized saved
baseline. Label-only trimming does not count as a change; add, delete, renumber,
type, max-length, weekday, local-time, and time-zone edits do. One `Save changes`
action writes both sections atomically and replaces local state and the baseline
with the complete backend-normalized response.

Cancel is disabled while Settings is loading, saving, or normalized-clean. When
either section has pending changes, Cancel asks before discarding them, restores
both last-loaded or last-saved sections together, clears superseded validation
interaction and save errors, and remains on `/settings`. Route exit uses the
same combined page-owned dirty state through `pendingChangesGuard` but
continues to the requested destination only after confirmation.

## Component Lab

`/component-lab` is a public manual demonstration surface for shared UI
components. It is not a production workflow or an alternative owner for shared
component documentation.

The lab includes interactive Basic and Disabled examples for the shared
`app-chip-list`. That component owns Material chip and autocomplete
presentation, controlled generic string-valued items, and accessible selection
and removal interactions. Provider lookup, paging, loading, errors, and numeric
category-ID mapping remain outside the shared component.

Pending-change confirmations explain which unsaved values will be lost.
`Keep editing` is ordered first and receives initial focus. `Discard changes`
uses the filled danger treatment. Escape and backdrop dismissal keep the edits
and restore focus to the action that opened the dialog.

## Route Protection And Ownership

`/calendar-events`, `/templates`, `/platforms`, and `/settings` use
`authenticatedGuard`. Both Calendar Event create and edit routes, Templates,
Platforms, and Settings also use `pendingChangesGuard`.

Route configuration belongs in `app.routes.ts`. Route-level page components
belong under `pages/`. Reusable presentation and form components belong under
`shared/`. Typed API mapping belongs in explicit client services, not route
configuration.
