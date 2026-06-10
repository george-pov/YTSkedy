# UI Deployment

The Angular frontend deploys to an Azure Storage static website. The storage
endpoint is a static public origin only; browser authentication remains in the
SPA and authorization remains enforced by the Azure Functions API.

## Target Host

Production UI hosting uses Azure Storage static website hosting:

| Item | Value |
| --- | --- |
| Resource group | `rg-ytskedy` |
| Storage account candidate | `stytskedyui` |
| Fallback name pattern | `stytskedyui<suffix>` |
| Static website container | `$web` |
| Index document | `index.html` |
| Error document | `index.html` |

Use the existing API resource group region for the first storage account. If
the candidate storage account name is unavailable, choose a fallback name that
uses only lowercase letters and numbers, then update the GitHub Environment
variable described below.

Azure Front Door, CDN, and custom domains are out of scope for this deployment
path.

## Workflow

The UI deployment workflow is:

```text
.github/workflows/deploy-azure-ui.yml
```

The workflow name is:

```text
Deploy Azure UI
```

The workflow runs on pushes to `main` and supports manual
`workflow_dispatch` runs with a GitHub Environment input. The default
environment is `production`.

The workflow uses GitHub OIDC with Azure RBAC. It must not use storage account
keys, connection strings, publish profiles, or function keys for routine
deployment.

## Build And Artifact Paths

The workflow builds the UI from:

```text
src/ui
```

Build and deploy paths:

| Item | Path or value |
| --- | --- |
| Node version | `24.x` |
| npm version | `11.13.0` |
| Build command | `npm run build` |
| Test command | `npm test` |
| Build output path | `src/ui/dist/ytskedy-ui/browser` |
| Deploy package path | `build/deploy/ytskedy-ui` |
| Artifact name | `ytskedy-ui-static-site` |
| Runtime config path | `config/app-config.json` |

The workflow uploads the built artifact, downloads it for deployment, writes
the GitHub-provided runtime config to `config/app-config.json`, removes
`config/app-config.sample.json`, then uploads the package to `$web`.

## GitHub Environment

Configure these values in the GitHub Environment used by the workflow:

| Name | Classification | Purpose |
| --- | --- | --- |
| `AZURE_CLIENT_ID` | Non-secret variable | Client ID for the existing `ytskedy-github-deploy` user-assigned managed identity. |
| `AZURE_TENANT_ID` | Non-secret variable | Azure tenant ID used for OIDC login. |
| `AZURE_SUBSCRIPTION_ID` | Non-secret variable | Azure subscription ID. |
| `AZURE_UI_RESOURCE_GROUP` | Non-secret variable | UI storage account resource group, currently `rg-ytskedy`. |
| `AZURE_UI_STORAGE_ACCOUNT_NAME` | Non-secret variable | Final UI storage account name. |
| `AZURE_UI_STATIC_WEBSITE_URL` | Non-secret variable | Primary static website endpoint returned by Azure. |
| `UI_APP_CONFIG_JSON` | Non-secret variable | Complete public `app-config.json` content for the deployed UI. |

`UI_APP_CONFIG_JSON` is browser-delivered public configuration. It can contain
API URLs, public Entra authority values, the SPA client ID, redirect URIs, and
OAuth scopes. It must not contain client secrets, access tokens, refresh
tokens, function keys, storage connection strings, account keys, SAS tokens,
passwords, or private certificates.

If a future deployment value is genuinely secret, store it as a GitHub
Environment secret and do not ship it to the browser.

The `UI_APP_CONFIG_JSON` value must match the runtime config contract:

```json
{
  "api": {
    "baseUrl": "https://<function-app-host>/"
  },
  "auth": {
    "clientId": "<spa-app-client-id-guid>",
    "authority": "https://<tenantSubdomain>.ciamlogin.com/<tenant-id-guid>/v2.0",
    "knownAuthorities": ["<tenantSubdomain>.ciamlogin.com"],
    "redirectUri": "https://<ui-origin>/",
    "postLogoutRedirectUri": "https://<ui-origin>/signed-out",
    "calendarEventsReadScope": "api://ytskedy-api/CalendarEvents.Read",
    "calendarEventsWriteScope": "api://ytskedy-api/CalendarEvents.Write"
  }
}
```

## Azure RBAC

Reuse the existing user-assigned managed identity:

```text
ytskedy-github-deploy
```

Grant it this role at the UI storage account scope:

```text
Storage Blob Data Contributor
```

The identity must have a federated credential for:

```text
repo:george-pov/YTSkedy:environment:production
```

Do not create a separate UI deployment identity unless the backend deployment
identity is intentionally retired.

## Entra Redirects

Register the deployed UI origin on the `YTSkedy SPA` app registration:

```text
<UI origin>/
<UI origin>/signed-out
```

The redirect URI in `UI_APP_CONFIG_JSON` must match the registered sign-in
redirect URI. The post-logout URI in `UI_APP_CONFIG_JSON` must match the
registered signed-out route.

## API CORS

The API enforces CORS in worker code through `CorsMiddleware` and `CorsPolicy`.
Do not configure platform-level Function App CORS for this API.

The durable configuration path is:

```text
Cors:AllowedOrigins:<index>
```

Keep local development at:

```text
Cors:AllowedOrigins:0=http://localhost:4200
```

Add the deployed UI origin without a trailing slash:

```text
Cors:AllowedOrigins:1=<UI origin without trailing slash>
```

In Azure Function App app settings, use double underscores so .NET
configuration binds the hierarchy:

```text
Cors__AllowedOrigins__1=<UI origin without trailing slash>
```

## Cache Behavior

The workflow clears `$web` after the new package exists locally and before
uploading the replacement files.

Upload order:

1. Versioned JavaScript, CSS, and asset files with long cache headers.
2. `config/app-config.json` with no-cache headers.
3. `index.html` with no-cache headers, uploaded last.

Uploading `index.html` last reduces the chance that a browser receives a new
HTML shell before the matching versioned assets exist.

## Validation

Local validation before relying on the workflow:

```powershell
cd src/ui
npm ci
npm run build
npm test
```

After deployment, smoke-check:

```text
<AZURE_UI_STATIC_WEBSITE_URL>
<AZURE_UI_STATIC_WEBSITE_URL>/config/app-config.json
<AZURE_UI_STATIC_WEBSITE_URL>/calendar-events
<AZURE_UI_STATIC_WEBSITE_URL>/signed-out
```

Then verify browser sign-in redirects use the deployed origin and that
authenticated API calls are not blocked by CORS. API `401` and `403` responses
must remain owned by the API authorization boundary.

## Rollback

For a bad UI deploy, rerun the workflow from the last known-good commit.

For a bad runtime config, correct `UI_APP_CONFIG_JSON`, upload only
`config/app-config.json`, and re-check the browser app.

For an origin mistake, remove the deployed origin from `YTSkedy SPA` redirects
and from the API CORS allow-list, then rerun the workflow with corrected
values.
