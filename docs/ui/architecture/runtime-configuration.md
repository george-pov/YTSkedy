# UI Runtime Configuration

The YTSkedy UI production approach is build once, configure per deployment.
The browser app consumes deployed backend APIs through runtime configuration.

Use runtime config for deploy-specific public settings:

- API base URLs
- public identity provider URLs
- public telemetry IDs
- feature flags
- support and integration URLs

Do not use runtime config for secrets or server-only settings.

## Active File Contract

The active config file is:

```text
src/ui/public/config/app-config.json
```

The active file is environment-specific and **gitignored**. A tracked
sample lives at:

```text
src/ui/public/config/app-config.sample.json
```

For local development on a fresh clone, copy the sample to `app-config.json`
and replace every `<placeholder>` value with environment-specific values:

```powershell
Copy-Item src/ui/public/config/app-config.sample.json `
          src/ui/public/config/app-config.json
```

The deployed environment supplies its own `app-config.json` during release.
For the Azure Storage deployment, GitHub stores the complete file content in
the `UI_APP_CONFIG_JSON` Environment variable. The deployment workflow writes
that value to the built artifact before upload. The gitignored local copy is
never committed.

Angular serves files from `public/` as static assets. At runtime the app
loads:

```text
config/app-config.json
```

The path should be resolved relative to the document base URI so the app can
still work when deployed below a subpath.

## Current Contract

The current runtime settings are the backend API base URL and public Entra
External ID browser settings:

```json
{
  "api": {
    "baseUrl": "https://ytskedy-api.example.com/"
  },
  "auth": {
    "clientId": "<spa-app-client-id-guid>",
    "authority": "https://<tenantSubdomain>.ciamlogin.com/<tenant-id-guid>/v2.0",
    "knownAuthorities": ["<tenantSubdomain>.ciamlogin.com"],
    "redirectUri": "http://localhost:4200",
    "postLogoutRedirectUri": "http://localhost:4200",
    "calendarEventsReadScope": "api://<api-app-id-uri-or-guid>/CalendarEvents.Read",
    "calendarEventsWriteScope": "api://<api-app-id-uri-or-guid>/CalendarEvents.Write"
  }
}
```

`api.baseUrl` and every `auth` value shown above are required. The app should
fail startup if the config file is missing or invalid.

## Deployment Source

The current production deployment does not use multiple tracked environment
templates. GitHub owns the complete deployed runtime config value:

```text
UI_APP_CONFIG_JSON
```

The workflow writes that value as-is to the built asset location:

```text
src/ui/dist/ytskedy-ui/browser/config/app-config.json
```

Only the active `app-config.json` under `public/config/` or the deployed
`config/app-config.json` should be served by the running app. Do not ship
`app-config.sample.json` in the deployed static website.

## Angular Boundary

Runtime config should be exposed through an app-owned injection token from:

```text
src/ui/src/app/shared/config/
```

Page-flow services should inject the typed config boundary instead of
hard-coding environment-specific values or importing build-time environment
files.

## Adding A Setting

When a new deploy-specific setting is needed:

1. Add the property to `AppConfig` or a nested config interface.
2. Update loader validation so required values fail fast at startup.
3. Update `src/ui/public/config/app-config.sample.json`.
4. Update the GitHub `UI_APP_CONFIG_JSON` value for deployed environments.
5. Inject the typed config boundary from application or service code.
6. Add or update focused tests for the loader and consuming service.

Build-time Angular environment files are reserved for compile-time behavior.
They are not the default place for deployment-specific runtime values.

## Security Rule

Runtime config is public browser data. It can contain API URLs, public
authority URLs, feature flags, and telemetry IDs. It must not contain secrets,
API keys, connection strings, passwords, private certificates, function keys,
OAuth client secrets, access tokens, or refresh tokens.

Specifically for calendar event API calls: the SPA authenticates via
Entra External ID as a public PKCE client (no client secret) and
attaches bearer access tokens through the YTSkedy-owned `AuthFacade` and
HTTP interceptor. Function keys (`x-functions-key`) are not accepted by
the API and must not appear in `app-config.json`, in code, or in tracked
`.http` files. Per-contributor bearer tokens used by manual checks
belong in the gitignored
`src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/CalendarEvents/http-client.env.json.user`,
not in runtime config.
