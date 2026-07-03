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

Publishing a calendar event to a YouTube platform creates a scheduled YouTube
live broadcast using Google OAuth credentials stored on the selected platform.
The backend exchanges the refresh token for short-lived access tokens at
runtime, so there is no interactive Google consent at request time.

YouTube does not use API host configuration keys in this slice. Operators enter
YouTube OAuth values through platform create or update requests:

| Field | Classification | Purpose |
| --- | --- | --- |
| `publishSettings.credentials.clientId` | Non-secret | Google OAuth 2.0 client identifier. |
| `publishSettings.credentials.clientSecret` | Secret | Google OAuth 2.0 client secret. |
| `publishSettings.credentials.refreshToken` | Secret | Long-lived refresh token minted once during setup with the YouTube scope. |
| `publishSettings.privacyStatus` | Non-secret | YouTube broadcast privacy, currently `private`, `public`, or `unlisted`. |
| `publishSettings.selfDeclaredMadeForKids` | Non-secret | YouTube made-for-kids flag sent to the broadcast status. |

`clientSecret` and `refreshToken` are accepted on platform create and update but
are never returned by platform reads. Responses return
`credentials.clientSecretConfigured` and
`credentials.refreshTokenConfigured` plus response-only display values instead.
`credentials.clientSecretDisplayValue` and
`credentials.refreshTokenDisplayValue` reveal only the final three stored
characters behind a fixed 12-character mask and hide the stored secret length.
On update, omitting either secret or sending it blank preserves the stored
value; sending a non-blank value replaces it. Redacted display values are not
accepted in create or update request bodies. `clientId` is returned because it
is not secret and is required on create and update.

For local manual checks, keep YouTube client secrets and refresh tokens in
`http-client.env.json.user`, not tracked `.http` environment files or
`local.settings.json`. See
[`../http/platforms.md`](../http/platforms.md) for the platform shape.

For the one-time procedure that creates the Google Cloud project, OAuth client,
and refresh token, see the setup runbook:
[`operations/youtube-publish-setup.md`](operations/youtube-publish-setup.md).

This is a proof-of-concept integration with deliberate limitations:

- Each channel's credentials are shared. Every publish through a platform acts
  on the single YouTube channel that minted that channel's refresh token,
  regardless of which user is signed in. Google OAuth is platform-scoped, not
  user-scoped.
- The YouTube client secret and refresh token are stored in the platform row's
  `PublishSettingsJson` so the provider can publish at request time. An app-managed
  secret store is not part of the current implementation.
- Only the rendered title, optional rendered description, scheduled start,
  privacy, and made-for-kids state are sent. Thumbnails, categories, and stream
  binding are out of scope for this slice.

## WordPress Publish Settings

Publishing a calendar event to a WordPress platform creates a post through the
WordPress REST API. WordPress does not use API host configuration keys in this
slice. Operators enter WordPress connection details through platform create or
update requests:

| Field | Classification | Purpose |
| --- | --- | --- |
| `publishSettings.siteUrl` | Non-secret | WordPress site root used to build `/wp-json/wp/v2/posts`. |
| `publishSettings.username` | Personal configuration | WordPress username used with an Application Password. |
| `publishSettings.applicationPassword` | Secret | WordPress Application Password sent through Basic Auth to the WordPress REST API. |
| `publishSettings.postStatus` | Non-secret | Initial WordPress post status, currently `draft` or `publish`. |

`applicationPassword` is accepted on platform create and update but is never
returned by platform reads. Responses return `applicationPasswordConfigured`
and `passwordDisplayValue` instead. `passwordDisplayValue` is always seven `*`
characters and reveals no stored characters. On update, omitting
`applicationPassword` or sending it blank preserves the stored value; sending a
non-blank value replaces it. Redacted display values are not accepted in create
or update request bodies.

For local manual checks, keep WordPress site URLs, usernames, and Application
Passwords in `http-client.env.json.user`, not tracked `.http` environment
files. Do not put WordPress Application Passwords in
`local.settings.sample.json`, tracked docs samples, logs, or source code.

WordPress site URL validation allows `http://localhost` and
`http://127.0.0.1` for local development. Every non-local WordPress site URL
must use HTTPS and must not include embedded credentials.

This is a first-slice integration with deliberate limitations:

- The WordPress Application Password is stored in the platform row's
  `PublishSettingsJson` so the provider can publish at request time. An app-managed
  secret store is not part of the current implementation.
- Only the rendered title and optional rendered description are sent.
  Categories, tags, excerpts, slugs, featured media, and WordPress scheduling
  are out of scope for this slice.

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

The host registers separate keyed table clients for templates, platforms,
platform publications, and application settings, each with its own table name
lookup and the same connection string lookup above:

1. `AzureStorage:TemplatesTableName`, default `Templates`.
2. `AzureStorage:PlatformsTableName`, default `Platforms`.
3. `AzureStorage:PlatformPublicationsTableName`, default `PlatformPublications`.
4. `AzureStorage:ApplicationSettingsTableName`, default `ApplicationSettings`.

The `ApplicationSettings` table stores application-owned settings such as the
current event text fields list. It is not an Azure Functions host settings
table and must not contain OAuth secrets, access tokens, refresh tokens, API
keys, or user-specific local configuration.

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

## Deployment Configuration Documentation

Document each deployment-required configuration value with:

- Setting name.
- Owner.
- Environment where it is required.
- Whether the value is public or secret.
- Local-development fallback, when one exists.
- Validation behavior when the value is missing or invalid.
