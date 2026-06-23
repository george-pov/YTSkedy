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
- `POST /api/calendar-events/{calendarEventId}/platforms/{platformId}/publish`
- `GET /api/platforms` and `GET /api/platforms?type={type}`
- `GET /api/platforms/{platformId}`
- `POST /api/platforms`
- `PUT /api/platforms/{platformId}`
- `DELETE /api/platforms/{platformId}`
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
that endpoint returns the calendar event detail shape
`{ calendarEventId, start: { localDateTime, timeZoneId }, scheduledStartUtc, descriptions, platforms }`
or `404` when the id is unknown. The `platforms` array carries one item per
active platform plus orphan history, so the detail read exposes publish state in
one call while the event itself stays provider-neutral. This is the only
endpoint that exposes per-event publication state. Save sends
`PUT /api/calendar-events/{calendarEventId}` with a body of
`{ descriptions: [{ language, title, description? }] }` and reads
`{ calendarEventId }` from the response. The scheduled start is immutable on
edit because the id is derived from it, so only the descriptions change. The
edit route also exposes a Delete action that calls
`DELETE /api/calendar-events/{calendarEventId}` and reads no body: it returns
`204 No Content` on success and `404 Not Found` when the id is unknown.

The `GET` endpoint returns a server-side sorted paged envelope
`{ items, page, pageSize, totalCount, sort, direction }`. The query carries
`page` (default `0`), `pageSize` (default `10`), `sort`
(`scheduledStart` | `timeZone` | `title`, default `scheduledStart`),
`direction` (`asc` | `desc`, default `desc`), and an optional both-or-neither
`year`/`month` filter. The default page is the first page sorted by scheduled
start descending. The UI consumes one page at a time and uses `totalCount` to
drive its paginator; it no longer scopes the list to a month. The canonical
parameter, envelope, and validation details live in
[`../api/http/calendar-events.md`](../api/http/calendar-events.md). The calendar
event list response is provider-neutral; the calendar event itself carries no
generic publish status or action flags. The single-event detail response
additionally embeds a `platforms` projection (per-platform publish state) as a
read-time composition, so publish state stays in platform publications rather
than on the event.

The `templates` resource exposes list, create, update, and delete, and a
separate `template-tokens` resource returns the code-defined placeholder token
list. The `Templates` page (`/templates`) consumes the list, create, update, and
delete endpoints through a typed `TemplatesService`; the `template-tokens`
resource is available to the client but is not yet surfaced in the editor. A
template carries a server-generated GUID `id`, a
`type` (`YouTube` or `WordPress`, immutable after create because it drives
storage partitioning), a `name` (required, at most 50 characters, unique within
a type), and free-text `content` (required, at most 2000 characters; tokens are
stored as-is and not validated in this slice). Create returns `200 OK` with the
new `id`, `400 Bad Request` on invalid input, and `409 Conflict` when the name
already exists in that type; update and delete locate by `{type}/{id}` and add
`404 Not Found`. Templates reuse the `CalendarEvents.Read` (GET) and
`CalendarEvents.Write` (POST, PUT, DELETE) scopes; no new scope was added. The
frontend bearer-token interceptor attaches an access token to the `templates`
and `template-tokens` URLs using those same scopes. The canonical request,
response, and error details live in
[`../api/http/templates.md`](../api/http/templates.md).

The `platforms` resource and the calendar-event publishing routes are the
multi-platform publishing contract. A `platform` is a configured publishing
destination (the first provider is YouTube) with a server-generated
`platformId`, a unique `name`, an immutable `type`, and non-secret
`publishSettings`. A calendar event is provider-neutral and has no publish
status; publish state is a `platform publication` keyed by calendar event and
platform. `GET /api/platforms` and the platform CRUD routes manage destinations; the
calendar event detail response (`GET /api/calendar-events/{calendarEventId}`)
returns one item per active platform (computed `NotPublished` when no row exists)
plus orphan history for deleted platforms in its `platforms` array; and
`POST /api/calendar-events/{calendarEventId}/platforms/{platformId}/publish`
publishes to one selected platform and returns the provider-neutral
`externalResourceId`. Deleting a platform preserves `Published` rows as
read-only orphan history and is blocked while any row is `Publishing`. These
routes reuse the `CalendarEvents.Read` (GET) and `CalendarEvents.Write` (POST,
PUT, DELETE, publish) scopes; no new scope was added. `publishSettings` and
publication rows never carry secret credential material; only a non-secret
credentials reference is stored. The canonical request, response, status-code,
and publishing-model details live in
[`../api/http/platforms.md`](../api/http/platforms.md). No UI consumes these
routes yet; the frontend calendar-event pages were built against the previous
provider-neutral calendar contract and platform publishing UI is follow-up work.

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
    `DELETE /api/calendar-events/{calendarEventId}`.
  - The `platforms`, event-platform listing, and publish routes reuse the same
    scopes: `CalendarEvents.Read` for `GET`, `CalendarEvents.Write` for `POST`,
    `PUT`, `DELETE`, and publish.
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
application-owned calendar event, template, platform, and platform-publication
rows. API persistence behavior is documented in
[`../api/persistence.md`](../api/persistence.md).

Persistence contracts are internal to the API boundary unless a feature
explicitly exposes them through HTTP.

## External Integrations

The calendar-event-level YouTube publish route was removed for the platform
publishing cutover. Publishing now goes through explicit platform endpoints:
`POST /api/calendar-events/{calendarEventId}/platforms/{platformId}/publish`
selects a configured platform and publishes through its provider adapter.
YouTube is the first implemented provider. The provider boundary is the
application port `IPlatformPublisher`, selected by platform type; YouTube SDK
types and credential resolution stay inside `YTSkedy.Infrastructure`. A
platform's non-secret `credentials` reference resolves to channel secrets bound
from the `YouTubeChannels` configuration section, which is never committed to
source control.

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
