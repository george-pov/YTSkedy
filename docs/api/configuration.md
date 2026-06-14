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

## YouTube Publish Settings

Publishing a calendar event as a scheduled YouTube live broadcast uses static,
predefined Google OAuth credentials. The backend exchanges the refresh token
for short-lived access tokens at runtime, so there is no interactive Google
consent at request time.

| Setting | Classification | Purpose |
| --- | --- | --- |
| `YouTube:ClientId` | Non-secret | Google OAuth 2.0 client identifier. |
| `YouTube:ClientSecret` | Secret | Google OAuth 2.0 client secret. |
| `YouTube:RefreshToken` | Secret | Long-lived refresh token minted once during setup with the YouTube scope. |
| `YouTubeBroadcast:PrivacyStatus` | Non-secret | Privacy applied to created broadcasts. Must be `private`, `public`, or `unlisted` (lowercase); other values fail validation on host start. Defaults to `private`. |
| `YouTubeBroadcast:SelfDeclaredMadeForKids` | Non-secret | Made-for-kids flag required by the API. Defaults to `false`. |

`YouTube:ClientSecret` and `YouTube:RefreshToken` are secrets. Never commit
real values; keep them in the ignored `local.settings.json` locally and in
hosted app settings or a secret store in deployed environments. The options
bind on host start and fail fast when a required `YouTube:` key is missing.
This is deliberate and host-wide: the API treats YouTube credentials as
required configuration, so a missing `YouTube:` key stops the whole Functions
host from starting, including the calendar-event list and create endpoints that
do not call YouTube. Running the API without publishing configured is not a
supported mode; do not remove the `ValidateOnStart` guard to boot a partial
host.

For the one-time procedure that creates the Google Cloud project, OAuth client,
and refresh token, see the setup runbook:
[`operations/youtube-publish-setup.md`](operations/youtube-publish-setup.md).

This is a proof-of-concept integration with deliberate limitations:

- All three credential values are shared. Every publish acts on the single
  YouTube channel that minted the refresh token, regardless of which user is
  signed in. A per-user Google OAuth flow is deferred.
- The broadcast defaults above apply to every publish. Only title, description,
  and scheduled start come from the calendar event.

## CORS

Browser bearer-token calls cross an origin boundary. CORS for the deployed
API is owned by Azure Functions platform CORS, not by API worker code and not
by deployment workflows. The worker no longer contains CORS middleware,
options, or a policy. Authentication and authorization remain API-owned; the
platform handles preflight `OPTIONS` and the `Access-Control-Allow-*`
response headers.

Platform CORS is managed manually in Azure. Deployment workflows must not add,
remove, or update CORS settings, and the Function App app settings must not
contain `Cors__AllowedOrigins__*` entries.

Allowed origins:

| Origin | Purpose |
| --- | --- |
| `http://localhost:4200` | Local Angular dev server. |
| `http://127.0.0.1:4201` | Local UI end-to-end test origin. |
| `<deployed-ui-origin>` | Deployed static website UI origin. |

Credentials behavior stays at the Azure default (`supportCredentials` is
`false`). Disallowed origins receive no `Access-Control-Allow-Origin` header,
so the browser blocks the call client-side.

Inspect and verify platform CORS with the Azure CLI:

```powershell
az functionapp cors show --name <function-app-name> --resource-group <resource-group> -o json
```

Add or remove an origin manually when the allow-list changes:

```powershell
az functionapp cors add --name <function-app-name> --resource-group <resource-group> `
  --allowed-origins "<origin without trailing slash>"

az functionapp cors remove --name <function-app-name> --resource-group <resource-group> `
  --allowed-origins "<origin without trailing slash>"
```

Environment-specific resource names and the deployed UI origin are recorded in
the local-only UI deployment operations runbook, not in durable documentation.

For local development with the Functions host, pass origins to
`func start --cors`; see
[`development/build-and-test.md`](development/build-and-test.md). Platform
CORS does not apply to the local `func` host.

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
