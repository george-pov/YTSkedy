# Integration Contracts

This document records cross-boundary contracts. Boundary implementation details
belong in `docs/api/` or `docs/ui/`.

## Frontend To Backend

The Angular UI consumes the backend through the Azure Functions REST API. The
current canonical API contract docs live under [`../api/http/`](../api/http/).

Current implemented HTTP surface:

- `POST /api/calendar-events`
- `GET /api/calendar-events?page={page}&pageSize={pageSize}&sort={sort}&direction={direction}`
- `GET /api/calendar-events/{calendarEventId}`
- `PUT /api/calendar-events/{calendarEventId}`
- `DELETE /api/calendar-events/{calendarEventId}`
- `POST /api/calendar-events/{calendarEventId}/publish`
- `GET /api/templates` and `GET /api/templates?type={type}`
- `POST /api/templates`
- `PUT /api/templates/{type}/{id}`
- `DELETE /api/templates/{type}/{id}`
- `GET /api/template-tokens`

Both list and create endpoints are consumed by the UI: the `CalendarEvents`
list page calls the `GET` endpoint, and the `CalendarEventDetails` create form
(`/calendar-events/new`) calls the `POST` endpoint with a body of
`{ start: { localDateTime, timeZoneId }, descriptions: [{ language, title, description? }] }`
and reads `{ calendarEventId }` from the response. The `CalendarEventDetails`
edit route (`/calendar-events/{calendarEventId}/edit`) calls
`GET /api/calendar-events/{calendarEventId}` to load an event into the form;
that endpoint returns a single item in the calendar event view shape
`{ calendarEventId, start: { localDateTime, timeZoneId }, scheduledStartUtc, descriptions, status }`
or `404` when the id is unknown. Save sends
`PUT /api/calendar-events/{calendarEventId}` with a body of
`{ descriptions: [{ language, title, description? }] }` and reads
`{ calendarEventId }` from the response. The scheduled start is immutable on
edit because the id is derived from it, so only the descriptions change, and the
endpoint accepts the edit only while the event is a `Draft` (a `Publishing` or
`Published` event returns `409 Conflict`). The edit route also exposes a Delete
action that calls `DELETE /api/calendar-events/{calendarEventId}` for an event
the API marks deletable and reads no body: it returns `204 No Content` on
success (a `Draft`, or a future `Published` event whose YouTube broadcast is
removed first), `404 Not Found` when the id is unknown or already gone,
`409 Conflict` when the event is not deletable in its current state (including a
future `Published` event with no recorded broadcast id), and `502 Bad Gateway`
when the YouTube broadcast delete fails and the local row is kept.

The `GET` endpoint returns a server-side sorted paged envelope
`{ items, page, pageSize, totalCount, sort, direction }`. The query carries
`page` (default `0`), `pageSize` (default `10`), `sort`
(`scheduledStart` | `status` | `timeZone` | `title`, default `scheduledStart`),
`direction` (`asc` | `desc`, default `desc`), and an optional both-or-neither
`year`/`month` filter. The default page is the first page sorted by scheduled
start descending. The UI consumes one page at a time and uses `totalCount` to
drive its paginator; it no longer scopes the list to a month. The canonical
parameter, envelope, and validation details live in
[`../api/http/calendar-events.md`](../api/http/calendar-events.md).

List and detail items carry a `status` field (`Draft`, `Publishing`, or
`Published`) and three server-computed action flags, `canPublish`, `canUpdate`,
and `canDelete`. The UI uses only these flags for Publish, edit/Save, and Delete
enablement and does not re-derive eligibility from `status`, scheduled start,
browser time, or descriptions. The list page shows a Publish action when
`canPublish` is `true`; it calls the publish endpoint (empty body) and reads
`{ calendarEventId, status, youTubeBroadcastId }`. New events are `Draft`; rows
stored before the field existed read as `Draft`. Publishing reserves the event
(`Draft` to `Publishing`) before the YouTube call so a second concurrent publish
is rejected; a failed broadcast releases the event back to `Draft`. The flags
are advisory: the backend re-checks eligibility on every publish, update, and
delete, so a stale flag is still enforced server-side.

Templates are an additive backend and API surface and are not consumed by the
UI in this slice. The `templates` resource exposes list, create, update, and
delete, and a separate `template-tokens` resource returns the code-defined
placeholder token list. A template carries a server-generated GUID `id`, a
`type` (`YouTube` or `WordPress`, immutable after create because it drives
storage partitioning), a `name` (required, at most 50 characters, unique within
a type), and free-text `content` (required, at most 2000 characters; tokens are
stored as-is and not validated in this slice). Create returns `200 OK` with the
new `id`, `400 Bad Request` on invalid input, and `409 Conflict` when the name
already exists in that type; update and delete locate by `{type}/{id}` and add
`404 Not Found`. Templates reuse the `CalendarEvents.Read` (GET) and
`CalendarEvents.Write` (POST, PUT, DELETE) scopes; no new scope was added. The
canonical request, response, and error details live in
[`../api/http/templates.md`](../api/http/templates.md).

The UI must treat API request and response shapes as integration contracts.
When a contract changes, update:

- The API HTTP contract doc.
- The API endpoint and DTO tests or manual checks.
- Any UI API client models, mapping tests, and affected route docs.

### Authentication And Authorization

Calendar event API calls cross an authentication boundary enforced in the
Azure Functions worker pipeline (not the Functions host key check).

- Every call must send `Authorization: Bearer <token>` with a Microsoft
  Entra External ID access token addressed to the `YTSkedy API` app
  registration (`Auth:ClientId` in
  [`../api/configuration.md`](../api/configuration.md)).
- Required scopes:
  - `CalendarEvents.Read` for `GET /api/calendar-events`.
  - `CalendarEvents.Write` for `POST /api/calendar-events`,
    `PUT /api/calendar-events/{calendarEventId}`,
    `DELETE /api/calendar-events/{calendarEventId}`, and
    `POST /api/calendar-events/{calendarEventId}/publish`.
- Required app role on every protected endpoint:
  `CalendarEvents.Operator` (in the `roles` claim).
- Frontend access tokens are obtained through MSAL via the YTSkedy-owned
  `AuthFacade`; a YTSkedy-owned HTTP interceptor attaches the bearer
  header for protected URLs. `MsalGuard` and `MsalInterceptor` are not
  used as the public boundary; consumers depend on the facade only so
  MSAL stays a swappable adapter.
- Function keys (`x-functions-key`) are not accepted on these endpoints
  and must not appear in frontend code, runtime config, or tracked
  `.http` files.

### Auth Error Behavior

| Status | Meaning |
| --- | --- |
| `401` | Missing bearer token, or invalid/expired/wrong-issuer/wrong-audience token. Response body is empty; the worker does not invoke `ChallengeAsync`, and `JwtBearerOptions.IncludeErrorDetails` is `false` (defense in depth). |
| `403` | Token is valid but the required scope is absent, or the `CalendarEvents.Operator` role is absent. Response body is empty. |

The UI maps `401` to a sign-in recovery flow and `403` to an authorization
message; the interceptor avoids infinite interactive-auth loops.

Cross-origin access is a separate concern owned by infrastructure, not the API
auth contract. CORS for the deployed API is configured in Azure Functions
platform CORS and managed manually; `401` and `403` behavior stays API-owned.
See [`../api/configuration.md`](../api/configuration.md) for the CORS model.

For backend validation internals and tenant configuration constraints (Entra
External ID issuer-host quirk, allow-list via Enterprise App Assignment
required), see [`../api/configuration.md`](../api/configuration.md).

## Scheduling Time

Scheduling behavior must use explicit date, time, and time-zone context.

- Submitted local date-time values must not silently depend on the local
  machine time zone.
- Persisted scheduled instants must be unambiguous.
- Repeated and skipped local times must have deliberate behavior.
- UI display should preserve API-provided local date-time and time-zone context
  unless a feature explicitly defines conversion behavior.

## Persistence

Azure Table Storage is the current persistence technology for
application-owned calendar event rows. API persistence behavior is documented
in [`../api/persistence.md`](../api/persistence.md).

Persistence contracts are internal to the API boundary unless a feature
explicitly exposes them through HTTP.

## External Integrations

YouTube live broadcast scheduling has an initial proof-of-concept integration.
`POST /api/calendar-events/{calendarEventId}/publish` creates a scheduled
YouTube `liveBroadcast` from static, shared Google OAuth credentials, and
`DELETE /api/calendar-events/{calendarEventId}` deletes that `liveBroadcast`
when a future `Published` event is removed. Both use the same `youtube` scope
and shared refresh token; no new scope or credential was added for deletion. It
is deliberately limited (broadcast insert and delete only, single shared
channel, no `liveStream` create/bind/cleanup, no thumbnail, no YouTube broadcast
lifecycle transition, no retry). The calendar event status is reserved (`Draft`
to `Publishing`) before the broadcast call to prevent duplicate broadcasts and
is set to `Published` only after the broadcast is created. A future `Published`
delete removes the YouTube broadcast before the local row, treats a YouTube
not-found as success-equivalent, and returns `502 Bad Gateway` (keeping the
local row) on any other YouTube failure. It is documented in
[`../api/http/calendar-events.md`](../api/http/calendar-events.md) and
[`../api/configuration.md`](../api/configuration.md).

WordPress, per-user OAuth, credential storage, and production telemetry remain
roadmap integration surfaces. Implementation must satisfy these requirements:

- Verify contract-sensitive behavior against official provider
  documentation.
- Keep provider DTOs and SDK behavior in infrastructure adapters.
- Keep OAuth secrets, access tokens, refresh tokens, API keys, and local
  credential stores out of source control.
- Make externally visible writes explicit and auditable.
- Add retry, rate-limit, idempotency, and recovery behavior appropriate for the
  provider.
- Add validation that proves the app can fail safely without exposing secrets
  or leaving ambiguous scheduled stream state.

## Contract Change Checklist

Before changing a contract, identify:

- Producer and consumer.
- Request shape, response shape, error shape, and status codes.
- Scheduling and time-zone semantics.
- Authorization, credential, and secret handling impact.
- Compatibility and rollback path.
- Validation command or manual check that covers the changed surface.
