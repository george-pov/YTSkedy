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

## Hosted Azure Functions Settings

The Bicep deployment supplies exactly these hosted Function settings. .NET
configuration maps double underscores to section separators, so hosted
`Auth__ClientId` is read as `Auth:ClientId` and
`AzureStorage__ConnectionString` is read as
`AzureStorage:ConnectionString`.

| Setting | Classification | Owner |
| --- | --- | --- |
| `AzureWebJobsStorage` | Secret connection string | Azure Functions host state and triggers. |
| `DEPLOYMENT_STORAGE_CONNECTION_STRING` | Secret connection string | Flex Consumption deployment package storage. |
| `AzureStorage__ConnectionString` | Secret connection string | Application tables and thumbnail blobs. |
| `AzureStorage__CalendarEventsTableName` | Non-secret | Calendar event table name. |
| `AzureStorage__TemplatesTableName` | Non-secret | Template table name. |
| `AzureStorage__ApplicationSettingsTableName` | Non-secret | Application settings table name. |
| `AzureStorage__PlatformsTableName` | Non-secret | Configured platform table name. |
| `AzureStorage__PlatformPublicationsTableName` | Non-secret | Platform publication table name. |
| `AzureStorage__ThumbnailsContainerName` | Non-secret | Calendar-event thumbnail container name. |
| `Auth__Instance` | Non-secret | External ID authority instance. |
| `Auth__TenantId` | Non-secret | External ID tenant. |
| `Auth__ClientId` | Non-secret | Environment API client ID and expected audience. |
| `Auth__Issuer` | Non-secret | Exact issuer from user-flow metadata. |
| `Auth__RequiredAppRole` | Non-secret | Required API app role value. |
| `PublicationExecution__OperationTimeoutSeconds` | Non-secret | Started provider-attempt deadline. |
| `PublicationExecution__FinalizationTimeoutSeconds` | Non-secret | Per-write finalization deadline. |
| `PublicationExecution__StaleAfterSeconds` | Non-secret | Minimum stale-attempt recovery age. |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Operational value | Environment Application Insights resource. |

Function host and deployment storage use one environment-specific storage
account. Application tables and blobs use a different environment-specific
storage account. Do not point `AzureStorage__ConnectionString` at Function host
storage in a hosted environment, and do not share either account between dev
and prod.

Connection string values, monitoring connection values, and concrete
environment identifiers belong in Azure configuration and local operations
records, not tracked documentation or workflow variables.

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

All `Auth:` keys are non-secret. The API does not require a client secret for
Entra External ID bearer-token validation. Hosted environments set the same
keys through their double-underscore app-setting forms listed above.

Entra External ID quirk: the authority host uses the tenant short subdomain
(`<tenant>.ciamlogin.com`) while the `iss` claim uses the tenant-GUID
subdomain. Set `Auth:Issuer` to the verbatim `issuer` value from the user
flow's **Endpoints** metadata when `Microsoft.Identity.Web`'s derived
issuer rejects valid tokens.

## YouTube Publish Settings

Publishing a calendar event to a YouTube platform creates a scheduled YouTube
live broadcast using Google OAuth credentials stored on the selected platform.
When the calendar event has a thumbnail, the backend applies that thumbnail to
the created broadcast after the local publication row records the broadcast id.
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
| `publishSettings.categoryId` | Non-secret | Optional opaque YouTube video category id. Null delegates category behavior to YouTube. |
| `publishSettings.containsSyntheticMedia` | Non-secret | Altered or synthetic content disclosure. Missing legacy values default to `false`. |

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

The browser labels null `categoryId` as `YouTube Default`. Its single-select
catalog is a static application-owned list of categories reviewed as assignable
for the US region. There is no runtime YouTube category route or provider read.
YouTube remains authoritative at publish time and may reject an id that is no
longer valid for the authenticated channel.

YouTube publication stages every scheduled broadcast as private. If category,
disclosure, or final visibility requires a video update, the backend reads only
the matching mutable video parts, copies those values into a safe update body,
and applies the configured fields before recording `Published`. A status update
always sends `containsSyntheticMedia` explicitly because later YouTube list
responses do not reliably return that field. The private, null-category, false-
disclosure case needs no video read or update.

Keep YouTube client secrets and refresh tokens out of tracked files, docs
samples, logs, and `local.settings.json`. See
[`http/platforms.md`](http/platforms.md) for the platform shape.

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
- The rendered title, optional rendered description, scheduled start, privacy,
  made-for-kids state, optional category, altered or synthetic disclosure, and
  optional event thumbnail are sent. Stream binding is not part of the current
  publish surface.
- Caught started failures are stored as retryable `Failed` without automatic
  provider deletion. Before retrying, the operator checks YouTube and deletes
  any uncertain broadcast directly when necessary.
- There is no thumbnail retry route or UI action. If broadcast creation
  succeeds but thumbnail application fails, the operator recovery path is to
  update the thumbnail in YouTube Studio.

## WordPress Publish Settings

Publishing a calendar event to a WordPress platform discovers the site's
WordPress REST API root, then creates a post through the WordPress REST API.
WordPress does not use API host configuration keys in this slice. Operators
enter WordPress connection details through platform create or update requests:

| Field | Classification | Purpose |
| --- | --- | --- |
| `publishSettings.siteUrl` | Non-secret | WordPress site root used for REST API discovery. |
| `publishSettings.username` | Personal configuration | WordPress username used with an Application Password. |
| `publishSettings.applicationPassword` | Secret | WordPress Application Password sent through Basic Auth to the WordPress REST API. |
| `publishSettings.postStatus` | Non-secret | Initial WordPress post status: `draft`, `pending`, `private`, `future`, or `publish`. |
| `publishSettings.sticky` | Non-secret | Whether WordPress treats the created post as sticky. |
| `publishSettings.scheduleOffsetHours` | Non-secret | Hour offset from `1` through `168` used only when `postStatus` is `future`; the provider `date_gmt` is computed from the calendar event scheduled start minus this offset. |
| `publishSettings.categoryIds` | Non-secret | Required ordered array of existing WordPress category term IDs. `[]` selects normal site default behavior. |

`applicationPassword` is accepted on platform create and update but is never
returned by platform reads. Responses return `applicationPasswordConfigured`
and `passwordDisplayValue` instead. `passwordDisplayValue` is always seven `*`
characters and reveals no stored characters. On update, omitting
`applicationPassword` or sending it blank preserves the stored value; sending a
non-blank value replaces it. Redacted display values are not accepted in create
or update request bodies.

Keep WordPress site URLs, usernames, and Application Passwords out of tracked
docs samples, logs, and source code. Do not put WordPress Application
Passwords in `local.settings.sample.json`.

WordPress site URL validation allows `http://localhost` and
`http://127.0.0.1` for local development. Every non-local WordPress site URL
must use HTTPS and must not include embedded credentials.

This is a first-slice integration with deliberate limitations:

- The WordPress Application Password is stored in the platform row's
  `PublishSettingsJson` so the provider can publish at request time. An app-managed
  secret store is not part of the current implementation.
- The publish request sends the rendered title, optional rendered description,
  configured WordPress status, sticky flag, conditional scheduled `date_gmt`,
  and every configured category ID. When `categoryIds` is `[]`, the backend
  omits the WordPress `categories` property so the site applies its normal
  default-category behavior. WordPress may accept a stale positive ID and drop
  it rather than rejecting the post, so operators must inspect the created
  post when verifying category assignment.
- Category lookup reuses the saved site URL and credentials through the backend.
  It adds no API host configuration key and exposes no WordPress credential to
  browser runtime configuration.
- Category creation, tags, custom taxonomies, excerpts, slugs, and featured
  media are not part of the current integration.

## Publication Execution

Publication attempts use three validated settings:

| Setting | Default | Purpose |
| --- | ---: | --- |
| `PublicationExecution:OperationTimeoutSeconds` | `120` | Maximum duration for a started provider attempt. The operation token also observes Functions host shutdown. |
| `PublicationExecution:FinalizationTimeoutSeconds` | `15` | Maximum duration of each independent final-state write. |
| `PublicationExecution:StaleAfterSeconds` | `300` | Minimum age before an active future `Publishing` row can be recovered by an operator. |

All values must be positive. `StaleAfterSeconds` must be strictly greater than
the operation timeout plus the finalization timeout. Invalid values fail host
startup validation. Azure Function App settings use double underscores, for
example `PublicationExecution__OperationTimeoutSeconds`.

Reads and publish preflight remain bound to request cancellation. The handler
switches to the server-owned operation scope immediately before it starts the
publication row. The named `YTSkedy.WordPress` HTTP client uses the same
operation timeout and has no automatic retry handler.

Cancellation telemetry distinguishes:

- a confirmed HTTP client disconnect, logged as Information by worker middleware
- provider operation timeout, logged with the operation-timeout source
- Functions host shutdown, logged with the host-shutdown source
- an unexpected provider cancellation with an uncanceled supplied token

No cancellation path logs authorization data, provider credentials, request
bodies, or raw platform settings.

## CORS

Browser bearer-token calls cross an origin boundary. CORS for the deployed
API is owned by Azure Functions platform CORS, not by API worker code and not
by deployment workflows. The worker no longer contains CORS middleware,
options, or a policy. Authentication and authorization remain API-owned; the
platform handles preflight `OPTIONS` and the `Access-Control-Allow-*`
response headers.

Platform CORS is managed manually in Azure. Deployment workflows must not add,
remove, or update CORS settings, and the Function App settings must not
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

The fallback supports local development and older hosts. Current hosted
environments always set a separate `AzureStorage:ConnectionString`; they must
not rely on `AzureWebJobsStorage` for application data.

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

The host registers a private Blob container client for calendar event thumbnail
bytes. It uses the same connection string lookup as the table clients:

1. `AzureStorage:ThumbnailsContainerName`
2. Default: `calendar-event-thumbnails`

The `ApplicationSettings` table stores application-owned settings such as the
current event text fields list and calendar event start defaults. It is not an
Azure Functions host settings table and must not contain OAuth secrets, access
tokens, refresh tokens, API keys, or user-specific local configuration.

## Function Authorization

Calendar event HTTP triggers run at `AuthorizationLevel.Anonymous`; the
Functions host no longer gates them. The security boundary is the worker
pipeline: `Microsoft.Identity.Web` validates the bearer token,
`[RequiredScope]` enforces the per-endpoint scope, and a workspace-wide
check enforces the `CalendarEvents.Operator` app role on every protected
endpoint.

Authenticated requests send `Authorization: Bearer <token>` with an Entra
External ID access token. Function keys do not apply to these endpoints; do not
pass `x-functions-key`.

Per-contributor bearer tokens and deployed host URLs belong outside tracked
configuration and documentation files.

## Deployment Configuration Documentation

Document each deployment-required configuration value with:

- Setting name.
- Owner.
- Environment where it is required.
- Whether the value is public or secret.
- Local-development fallback, when one exists.
- Validation behavior when the value is missing or invalid.
