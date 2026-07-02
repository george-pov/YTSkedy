# Integration Contracts

This document records cross-boundary contracts and ownership. Endpoint route,
request, response, status-code, and manual-check details belong in
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
| Event text fields settings reads and writes | Azure Functions API | `Settings` and `CalendarEventDetails` pages | [`../api/http/calendar-events.md`](../api/http/calendar-events.md) |
| Platform CRUD, `referenceKey`, publish, publication delete, and provider-specific settings | Azure Functions API | `Platforms` and `CalendarEventDetails` pages | [`../api/http/platforms.md`](../api/http/platforms.md) |
| Template CRUD and template-token reads | Azure Functions API | `Templates` page and template editor clients | [`../api/http/templates.md`](../api/http/templates.md) |
| Browser routes, page orchestration, and client interaction state | Angular UI | Browser users and API contract consumers checking UI behavior | [`../ui/routes.md`](../ui/routes.md) |
| Runtime browser configuration | Deployment environment serving `app-config.json` | Angular config loader and typed API clients | [`../ui/architecture/runtime-configuration.md`](../ui/architecture/runtime-configuration.md) |
| Bearer-token authentication and authorization | Entra External ID plus API worker middleware | Angular `AuthFacade`, bearer interceptor, and manual clients | This document and [`../api/configuration.md`](../api/configuration.md) |
| Scheduling instants and time-zone interpretation | Backend API | Angular UI and external provider adapters | This document and [`../api/http/calendar-events.md`](../api/http/calendar-events.md) |
| Application persistence shape | API infrastructure adapters | API application code; UI only through HTTP projections | [`../api/persistence.md`](../api/persistence.md) |
| External provider publishing and cleanup | API application ports and infrastructure adapters | HTTP publish and publication-delete routes | [`../api/http/platforms.md`](../api/http/platforms.md) and [`../api/operations/`](../api/operations/) |

## Frontend To Backend

The Angular UI consumes the backend through the Azure Functions REST API. The
canonical HTTP contract docs live under [`../api/http/`](../api/http/).

The UI must treat API request and response shapes as integration contracts. UI
code should consume them through typed services and explicit mapping code, not
through route configuration or ad hoc template logic. UI docs may describe which
page consumes a route, but they should link to API HTTP contract docs instead of
duplicating endpoint shapes.

Cross-boundary rules:

- Clients must use backend-computed action flags, such as `canPublish` and
  `canDeletePublication`, rather than re-deriving eligibility from browser time,
  provider ids, or local status checks.
- Calendar event create uses the current event text fields setting, while
  calendar event list and details expose each event's stored text snapshot.
  Clients must not reshape edit forms from the current setting.
- Calendar event list responses are provider-neutral. Per-platform publication
  state is exposed through the calendar event details read model.
- The Settings page consumes `GET /api/settings/event-text-fields` and
  `PUT /api/settings/event-text-fields` through a typed settings service. The
  backend owns `fieldKey` derivation and normalizes keys from field order.
- Template-token reads expose current `textN` fields, fixed date tokens, and
  active platform `referenceKey` values for template authoring. Preview and
  publish render from the selected calendar event's stored text snapshot and
  already-published platform external resource ids for matching reference-key
  tokens.
- Platform publish and publication cleanup always target an explicit platform.
  There is no calendar-event-level publish route.
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
application-settings rows. API persistence behavior is documented in
[`../api/persistence.md`](../api/persistence.md).

Persistence contracts are internal to the API boundary unless a feature
explicitly exposes them through HTTP. The UI must not depend on table names,
partition keys, row keys, ETags, or storage-specific conflict behavior except
through documented HTTP responses.

## External Integrations

External provider work stays behind API-owned application ports and
infrastructure adapters. The UI triggers provider work only through documented
HTTP routes.

- YouTube SDK types, WordPress REST DTOs, and provider credential handling stay
  inside `YTSkedy.Infrastructure`.
- Publishing uses `IPlatformPublisher` selected by platform type.
- Publication cleanup uses `IPlatformPublicationDeleter` selected by platform
  type.
- Provider-specific request mapping, cleanup behavior, and recovery notes are
  documented in [`../api/http/platforms.md`](../api/http/platforms.md) and
  operation runbooks under [`../api/operations/`](../api/operations/).
- Provider secrets, OAuth tokens, access tokens, refresh tokens, API keys, and
  local credential stores must not be committed, logged, returned by read
  models, or stored in browser runtime configuration.

Per-user OAuth, credential migration to a dedicated secret store, retry,
rate-limit handling, reconciliation, and production telemetry remain roadmap
integration surfaces.

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
