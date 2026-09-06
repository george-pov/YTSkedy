# Integration Contracts

This document records cross-boundary contracts and ownership. Endpoint route,
request, response, status code, and manual check details belong in
[`../api/http/`](../api/http/). UI route and page behavior belongs in
[`../ui/routes.md`](../ui/routes.md). Boundary implementation details belong in
`docs/api/` or `docs/ui/`.

## Ownership

- Source of truth: producer and consumer ownership for cross-boundary contract
  surfaces, plus shared auth, scheduling, persistence, and integration rules.
- Update when: a contract surface changes producer, consumer, durable owner,
  auth model, scheduling interpretation, persistence exposure, or provider
  boundary.
- Validate with: confirm endpoint details stay in API HTTP docs, UI page
  behavior stays in UI route docs, provider runbook details stay in API
  operations docs, and run `git diff --check`.

## Producer And Consumer Matrix

| Contract surface | Producer | Consumer | Durable owner |
| --- | --- | --- | --- |
| HTTP API routes, DTOs, status codes, and auth requirements | Azure Functions API | Angular UI typed services and manual API clients | [`../api/http/`](../api/http/) |
| Calendar event list, details, create, update, and delete behavior | Azure Functions API | `CalendarEvents` and `CalendarEventDetails` pages | [`../api/http/calendar-events.md`](../api/http/calendar-events.md) |
| Calendar event defaults reads and atomic writes | Azure Functions API | `Settings` and create-mode `CalendarEventDetails` pages | [`../api/http/calendar-event-defaults.md`](../api/http/calendar-event-defaults.md) |
| Calendar event start suggestions | Azure Functions API | Create-mode `CalendarEventDetails` page | [`../api/http/calendar-events.md`](../api/http/calendar-events.md) |
| Calendar-event thumbnail storage and retrieval | Azure Functions API | `CalendarEventDetails` page and provider publication flow | [`../api/http/calendar-event-thumbnails.md`](../api/http/calendar-event-thumbnails.md) |
| Platform CRUD, `referenceKey`, and provider-specific settings | Azure Functions API | `Platforms` page | [`../api/http/platforms.md`](../api/http/platforms.md) |
| Saved WordPress platform category lookup | Azure Functions API and WordPress REST API | `Platforms` page category selector | [`../api/http/platforms.md`](../api/http/platforms.md) |
| Publishing-content preview, publish, publication state, and publication delete | Azure Functions API | `CalendarEventDetails` page | [`../api/http/platform-publications.md`](../api/http/platform-publications.md) |
| Template CRUD and template-token reads | Azure Functions API | `Templates` page and template editor clients | [`../api/http/templates.md`](../api/http/templates.md) |
| Browser routes, page orchestration, and client interaction state | Angular UI | Browser users and API contract consumers checking UI behavior | [`../ui/routes.md`](../ui/routes.md) |
| Runtime browser configuration | Deployment environment serving `app-config.json` | Angular config loader and typed API clients | [`../ui/architecture/runtime-configuration.md`](../ui/architecture/runtime-configuration.md) |
| Bearer-token authentication and authorization | Entra External ID plus API worker middleware | Angular `AuthFacade`, bearer interceptor, and manual clients | This document and [`../api/configuration.md`](../api/configuration.md) |
| Scheduling instants and time-zone interpretation | Backend API | Angular UI and external provider adapters | This document and [`../api/http/calendar-events.md`](../api/http/calendar-events.md) |
| Application persistence shape | API infrastructure adapters | API application code; UI only through HTTP projections | [`../api/persistence.md`](../api/persistence.md) |
| External provider publishing and cleanup | API application ports and infrastructure adapters | HTTP publish and publication-delete routes | [`../api/http/platform-publications.md`](../api/http/platform-publications.md) and [`../api/operations/`](../api/operations/) |

## Frontend To Backend

The Angular UI consumes the backend through the Azure Functions REST API. The
canonical HTTP contract docs live under [`../api/http/`](../api/http/).

The UI must treat API request and response shapes as integration contracts. UI
code should consume them through typed services and explicit mapping code, not
through route configuration or ad hoc template logic. UI docs may describe which
page consumes a route, but they should link to API HTTP contract docs instead of
duplicating endpoint shapes.

Cross-boundary rules:

- Clients must use backend-computed action flags, such as root `canUpdate` and
  `canDelete` on calendar event details and row-level `canPublish` and
  `canDeletePublication`, rather than re-deriving eligibility from browser
  time, provider ids, row status, or local row counts.
- Calendar event create uses the current event text fields setting, while
  calendar event list and details expose each event's stored text snapshot.
  Clients must not reshape edit forms from the current setting.
- Calendar event list responses remain provider-neutral and include required
  informational `publicationStatus` values `NotPublished`,
  `PartiallyPublished`, `FullyPublished`, or `Failed`. The API calculates this
  aggregate from the event's derived published-platform-id index and the
  platforms active at list-read time. The UI maps the tokens to page-owned
  labels and may request server-side sorting by the aggregate, but must not use
  it for action eligibility. The list-level `Failed` value remains reserved and
  is not derived from per-platform failed-attempt rows.
- Per-platform publication state and root event mutation flags are exposed
  through the calendar event details read model. An active future `Failed` row
  is retryable after operator verification, may retain an external resource id,
  exposes its stored content snapshot, and cannot use normal publication
  delete. A row also exposes `publicationUpdatedUtc` and the backend-computed
  `canRecoverPublication` flag. Clients must not calculate staleness. Details do
  not include the list aggregate and continue to use authoritative publication
  rows.
- Calendar event update requests include both `start` and `texts`. The backend
  owns scheduled-start conversion, invalid/repeated local-time validation,
  publication-lock enforcement, and best-effort duplicate scheduled-start
  detection. The UI enables or disables scheduled-start and event-text controls
  from the API-provided `canUpdate` flag.
- The Settings page consumes the combined calendar event defaults contract
  through one typed service and one Save action. The backend owns `fieldKey`
  derivation, normalizes keys from field order, validates both settings
  sections, and writes both persistence rows atomically.
- The create page may send its supported browser time zone as a start-suggestion
  fallback, but the backend owns effective-zone priority, current time, weekly
  local-date selection, DST validation, and UTC collision checks. Suggestions
  are advisory and never replace create-time duplicate validation.
- Template-token reads expose current `textN` fields, fixed date tokens, and
  active platform `referenceKey` values for template authoring. Preview and
  publish render from the selected calendar event's stored text snapshot and
  already-published platform external resource ids for matching reference-key
  tokens.
- Platform publish and publication cleanup always target an explicit platform.
  Their responses are row-level only, so clients refresh calendar event details
  when they need root event action flags after those mutations. There is no
  calendar-event-level publish route.
- Platform CRUD exposes the optional provider-neutral `referenceKey` field.
  The backend owns validation, blank-as-null normalization, case-insensitive
  uniqueness, and the `409 Conflict` duplicate-key response. The UI consumes it
  through typed platform models and must not infer provider-specific roles from
  it. When used as a template token name, a matching `referenceKey` resolves to
  that platform publication's `externalResourceId` only after the platform is
  published for the selected calendar event.
- Platform CRUD requires both `publishingContent.titleTemplateId` and
  `publishingContent.descriptionTemplateId`. There is no `(none)` option and no
  direct text-field fallback during preview or publish.
- WordPress platform CRUD carries required non-null `categoryIds`. Category
  names and slugs are provider lookup data only. The `Platforms` page searches
  them through the protected backend route after the platform is saved; browser
  code never receives WordPress credentials or calls WordPress directly.
- YouTube platform CRUD carries nullable opaque `categoryId`,
  `defaultAudioLanguage`, and `defaultLanguage` values plus the boolean
  `containsSyntheticMedia` disclosure. Null category or language means provider
  default. Missing legacy values read as null for category and languages and
  false for the disclosure.
- The `Platforms` page owns the static reviewed YouTube category and language
  catalogs. Stream language and title and description language are separate
  catalogs because only the stream catalog includes `zxx` (`Not applicable`).
  The UI does not call YouTube or the backend for runtime discovery and
  preserves unknown stored language codes until the operator changes them.
- Secret-bearing settings may be accepted by write routes, but read models must
  return redacted configuration flags instead of secrets.
- Function keys are not part of the frontend-backend contract.

When a frontend-backend contract changes, update:

- The canonical API HTTP contract doc.
- API endpoint, DTO, mapping, or manual-check coverage.
- UI API client models, mapping tests, and affected route docs.

## Authentication And Authorization

Protected API calls cross an authentication boundary enforced in the Azure
Functions worker pipeline, not the Functions host key check.

- Every protected call must send `Authorization: Bearer <token>` with a
  Microsoft Entra External ID access token addressed to the `YTSkedy API` app
  registration (`Auth:ClientId` in
  [`../api/configuration.md`](../api/configuration.md)).
- Required scopes:
  - `CalendarEvents.Read` for `GET` resources.
  - `CalendarEvents.Write` for `POST`, `PUT`, `DELETE`, publish, and
    publication delete resources.
- Required app role on every protected endpoint:
  `CalendarEvents.Operator` in the `roles` claim.
- Frontend access tokens are obtained through MSAL via the YTSkedy-owned
  `AuthFacade`. A YTSkedy-owned HTTP interceptor attaches the bearer header for
  protected URLs.
- `MsalGuard` and `MsalInterceptor` are not the public app boundary. Consumers
  depend on the YTSkedy facade so MSAL stays a swappable adapter.
- Function keys (`x-functions-key`) are not accepted on protected endpoints and
  must not appear in frontend code, runtime config, or tracked `.http` files.

### Auth Error Behavior

| Status | Meaning |
| --- | --- |
| `401` | Missing bearer token, or invalid, expired, wrong-issuer, or wrong-audience token. Response body is empty; the worker does not invoke `ChallengeAsync`, and `JwtBearerOptions.IncludeErrorDetails` is `false`. |
| `403` | Token is valid but the required scope is absent, or the `CalendarEvents.Operator` role is absent. Response body is empty. |

The UI maps `401` to sign-in recovery and `403` to an authorization message.
The interceptor avoids infinite interactive-auth loops.

Cross-origin access is a separate infrastructure concern. CORS for the deployed
API is configured in Azure Functions platform CORS and managed manually; `401`
and `403` behavior stays API-owned. See
[`../api/configuration.md`](../api/configuration.md) for the CORS model and
tenant validation constraints.

## Scheduling Time

Scheduling behavior must use explicit date, time, and time-zone context.

- Submitted local date-time values must not silently depend on the local
  machine time zone.
- Persisted scheduled instants must be unambiguous.
- Repeated and skipped local times must have deliberate behavior.
- UI display should preserve API-provided local date-time and time-zone context
  unless a feature explicitly defines conversion behavior.
- External provider writes must use backend-owned scheduling interpretation, not
  browser-local time assumptions.

## Persistence

Azure Table Storage is the current persistence technology for
application-owned calendar event, template, platform, platform-publication, and
application-settings rows. Private Azure Blob Storage stores calendar event
thumbnail bytes. API persistence behavior is documented in
[`../api/persistence.md`](../api/persistence.md).

Persistence contracts are internal to the API boundary unless a feature
explicitly exposes them through HTTP. The UI must not depend on table names,
partition keys, row keys, ETags, or storage-specific conflict behavior except
through documented HTTP responses.

Calendar-event update and thumbnail metadata mutations use conditional ETag
writes. A lost ETag race is an HTTP `409 Conflict`, and the client must reload
the event before retrying. A missing row remains `404 Not Found`. The
application and domain projects exchange storage-neutral change results, so
Azure SDK request failures do not cross the infrastructure boundary.

Calendar-event rows carry a secret-free derived set of successfully published
platform ids only to support the list aggregate. `PlatformPublications` remains
the authoritative source for per-platform state, actions, mutation locks,
publish, and cleanup. Platform lifecycle changes do not fan out writes to
calendar events; comparing with current active platform ids may reclassify past
list rows. A derived-index failure is logged after the authoritative operation
and does not change the publish or publication-delete HTTP result.

## External Integrations

External provider work stays behind API-owned application ports and
infrastructure adapters. The UI triggers provider work only through documented
HTTP routes.

- YouTube SDK types, WordPress REST DTOs, and provider credential handling stay
  inside `YTSkedy.Infrastructure`.
- Publishing uses `IPlatformPublisher` selected by platform type.
- Reads and publish preflight use the HTTP request token. Immediately before the
  conditional `Publishing` write, the handler switches to a server-owned
  operation token bounded by configuration and host shutdown. Final-state
  writes use fresh, independently bounded tokens.
- YouTube publication creates a private scheduled broadcast first. When
  category, default audio language, default title and description language,
  disclosure, or final visibility requires a video update, the adapter reads
  only the included mutable parts, preserves their values, and applies
  YTSkedy-owned values before the local row becomes `Published`. The language
  update shares the existing replacement-safe read and single update call.
- YouTube checkpoints the broadcast id immediately after insert and before
  later video metadata work. WordPress checkpoints the post id after validating
  the create response. Checkpoint and final-state writes are conditional.
- Handled started failures, including bounded cancellation, are recorded as
  `Failed` without automatic provider deletion. A known external id is retained
  for operator verification, and retry conditionally replaces only that failed
  row with the transient `Publishing` concurrency guard.
- Hard termination may still leave `Publishing`. Recovery is an explicit
  authenticated write based on backend-computed age and an exact
  timestamp-plus-ETag conditional transition. It never deletes a provider
  resource.
- Thumbnail application uses `IThumbnailPublisher` selected by platform type.
- Publication cleanup uses `IPlatformPublicationDeleter` selected by platform
  type.
- WordPress category lookup uses an application reader port and infrastructure
  adapter. It reuses backend-owned endpoint discovery and Basic Auth, and it
  performs provider reads only.
- Successful WordPress REST API discovery is cached for five minutes per API
  process. Calls for the same site share discovery work, and failed discovery
  is not cached.
- Every WordPress HTTP request identifies the client as `YTSkedy/1.0`. This
  identification applies to endpoint discovery and authenticated operations;
  it does not replace WordPress authentication or authorization.
- Each publish attempt has a generated reference id. WordPress requests also
  carry it as `X-YTSkedy-Request-Id`, and structured logs correlate it with the
  request stage, status, duration, endpoint style, discovery cache use, and
  provider request count.
- A handled failure stores a secret-safe diagnostic summary on the failed
  publication row and returns the same summary through the publish error
  contract. WordPress error codes and `Retry-After` may be retained; raw
  response bodies, provider messages, authorization headers, credentials, and
  publishing content are excluded.
- Provider-specific request mapping, cleanup behavior, and recovery notes are
  documented in
  [`../api/http/platform-publications.md`](../api/http/platform-publications.md)
  and
  operation runbooks under [`../api/operations/`](../api/operations/).
- Provider secrets, OAuth tokens, access tokens, refresh tokens, API keys, and
  local credential stores must not be committed, logged, returned by read
  models, or stored in browser runtime configuration.

Per-user OAuth, credential migration to a dedicated secret store, retry,
rate-limit handling, reconciliation, and production telemetry are not part of
the current integration surface.

## Contract Change Checklist

Before changing a contract, identify:

- Producer and consumer.
- Canonical durable owner for the contract.
- Request shape, response shape, error shape, and status codes.
- Scheduling and time-zone semantics.
- Authorization, credential, and secret-handling impact.
- Persistence, provider, and recovery impact.
- Compatibility and rollback path.
- API tests, UI mapping tests, build command, or manual check that covers the
  changed surface.

After changing a contract, update:

- API HTTP contract docs for endpoint details.
- UI route docs for page consumption behavior.
- API endpoint, DTO, and mapping tests or manual `.http` checks.
- UI typed client models, mapping tests, and route behavior tests when affected.
- Operation runbooks when provider cleanup, recovery, deployment, or secret
  handling changes.
