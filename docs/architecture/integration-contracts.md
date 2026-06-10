# Integration Contracts

This document records cross-boundary contracts. Boundary implementation details
belong in `docs/api/` or `docs/ui/`.

## Frontend To Backend

The Angular UI consumes the backend through the Azure Functions REST API. The
current canonical API contract docs live under [`../api/http/`](../api/http/).

Current implemented HTTP surface:

- `POST /api/calendar-events`
- `GET /api/calendar-events?year={year}&month={month}`

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
  - `CalendarEvents.Write` for `POST /api/calendar-events`.
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

YouTube API, OAuth, WordPress, credential storage, and production telemetry are
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
