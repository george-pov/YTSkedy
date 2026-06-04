# UI Runtime Configuration

The YTSkedy UI production approach is build once, configure per deployment.
Runtime configuration is a required release capability before the browser app
consumes deployed backend APIs.

Use runtime config for deploy-specific public settings:

- API base URLs
- public identity provider URLs
- public telemetry IDs
- feature flags
- support and integration URLs

Do not use runtime config for secrets or server-only settings.

## Active File Contract

The active config file should be:

```text
src/ui/public/config/app-config.json
```

Angular serves files from `public/` as static assets. At runtime the app should
load:

```text
config/app-config.json
```

The path should be resolved relative to the document base URI so the app can
still work when deployed below a subpath.

## Initial Contract

The first expected runtime setting is the backend API base URL:

```json
{
  "api": {
    "baseUrl": "https://ytskedy-api.example.com"
  }
}
```

`api.baseUrl` is required and must be a non-empty string. The app should fail
startup if the config file is missing or invalid.

## Environment Templates

When multiple deployment targets exist, keep templates outside `public/`:

```text
src/ui/config/environments/app-config.dev.json
src/ui/config/environments/app-config.qa.json
src/ui/config/environments/app-config.prod.json
```

Only the active `app-config.json` under `public/config/` should be served by
the running app. Deployment should copy the correct environment config to the
built asset location:

```text
src/ui/dist/ytskedy-ui/browser/config/app-config.json
```

Use the same Angular build artifact for each environment when possible. Change
only `app-config.json` during release or deployment.

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
3. Update `src/ui/public/config/app-config.json`.
4. Update every template under `src/ui/config/environments/`.
5. Inject the typed config boundary from application or service code.
6. Add or update focused tests for the loader and consuming service.

Build-time Angular environment files are reserved for compile-time behavior.
They are not the default place for deployment-specific runtime values.

## Security Rule

Runtime config is public browser data. It can contain API URLs, public
authority URLs, feature flags, and telemetry IDs. It must not contain secrets,
API keys, connection strings, passwords, private certificates, function keys,
OAuth client secrets, access tokens, or refresh tokens.
