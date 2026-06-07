# API Configuration

API runtime configuration belongs outside source control when values are
environment-specific or secret.

## Local Settings

The ignored Azure Functions local settings file is:

```text
src/api/YTSkedy.AzureFunctions/local.settings.json
```

The tracked placeholder template is:

```text
src/api/YTSkedy.AzureFunctions/local.settings.sample.json
```

For local development, copy the sample to `local.settings.json` and replace
every `<placeholder>` value with environment-specific values.

Do not commit OAuth client secrets, refresh tokens, access tokens, API keys,
storage connection strings for real accounts, or local credential stores.

## Entra External ID Settings

The Entra External ID authentication configuration uses these local settings
keys:

| Setting | Classification | Purpose |
| --- | --- | --- |
| `Auth:Instance` | Non-secret | Authority instance URL used for Entra metadata. |
| `Auth:TenantId` | Non-secret | Entra External ID tenant identifier. |
| `Auth:ClientId` | Non-secret | API app registration client ID and expected token audience. |
| `Auth:Issuer` | Non-secret | Expected issuer URL when issuer validation needs the exact metadata value. |
| `Auth:RequiredAppRole` | Non-secret | Required app role value, currently `CalendarEvents.Operator`. |

All `Auth:` keys are non-secret. The API does not require a client secret
for Entra External ID bearer-token validation. Hosted environments should
set the same keys through app settings.

Entra External ID quirk: the authority host uses the tenant short subdomain
(`<tenant>.ciamlogin.com`) while the `iss` claim uses the tenant-GUID
subdomain. Set `Auth:Issuer` to the verbatim `issuer` value from the user
flow's **Endpoints** metadata when `Microsoft.Identity.Web`'s derived
issuer rejects valid tokens.

## CORS

Browser bearer-token calls cross an origin boundary; CORS is enforced in
worker code (`CorsMiddleware` ahead of `BearerTokenMiddleware`), not via
the Functions host's `--cors` flag, so preflight `OPTIONS` returns 204
without invoking the authentication pipeline.

| Setting | Classification | Purpose |
| --- | --- | --- |
| `Cors:AllowedOrigins:<index>` | Non-secret | Allow-listed browser origins. Seed `http://localhost:4200` locally; add the deployed SPA origin per environment. |

The policy allows headers `Content-Type, Authorization`, methods
`GET, POST, OPTIONS`, and does not enable `AllowCredentials`. Disallowed
origins receive no CORS headers and the browser blocks the call
client-side.

Do not configure CORS through the Functions host's `Host:CORS` or
`func start --cors ...`; those paths short-circuit the worker pipeline
and bypass `CorsMiddleware`'s contract with authentication.

## Azure Storage

Calendar event persistence reads the storage connection string in this order:

1. `AzureStorage:ConnectionString`
2. `AzureWebJobsStorage`

For local Azurite development, set:

```json
{
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true"
  }
}
```

Calendar event table name lookup:

1. `AzureStorage:CalendarEventsTableName`
2. Default: `CalendarEvents`

## Function Authorization

Calendar event HTTP triggers run at `AuthorizationLevel.Anonymous`; the
Functions host no longer gates them. The security boundary is the worker
pipeline: `Microsoft.Identity.Web` validates the bearer token,
`[RequiredScope]` enforces the per-endpoint scope, and a workspace-wide
check enforces the `CalendarEvents.Operator` app role on every protected
endpoint.

Local manual checks send `Authorization: Bearer <token>` with an Entra
External ID access token obtained via the `az`-based recipe in
`docs/api/development/build-and-test.md`. Function keys do not apply to
these endpoints; do not pass `x-functions-key`.

Per-contributor bearer tokens and deployed host URLs belong in
`http-client.env.json.user`, not in tracked `.http` environment files.

## Production Configuration Requirements

Production configuration must define:

- YouTube OAuth client IDs and callback behavior.
- Credential store location or provider.
- Default channel assumptions.
- API base URL behavior for the frontend host.
- Feature flags for dry-run or preview flows.
- Telemetry settings that do not expose secrets or personal account data.

Configuration values required for deployment must be documented with:

- setting name
- owner
- environment where it is required
- whether the value is public or secret
- local-development fallback, when one exists
- validation behavior when the value is missing or invalid
