# UI Architecture

The Angular frontend lives under `src/ui/`.

## Current State

- `src/ui/` contains an Angular workspace managed with npm.
- The frontend package is named `ytskedy-ui`.
- `package.json` declares `npm@11.13.0` as the frontend package manager.
- Angular packages currently use version `22.0.0` ranges.
- TypeScript currently uses `~6.0.2`.
- Angular Material and Angular CDK are available UI dependencies. Repeated or
  app-owned Material usage should be isolated behind shared components under
  `src/ui/src/app/shared/components/`.
- Styling uses SCSS.
- Routing is configured through Angular router.
- Routed pages render through the `AppLayout` route shell.
- The app has a `calendar-events` page route. The page is currently an initial
  route component and does not call the backend API.

## Source Layout

Current application source lives under:

```text
src/ui/src/app/
```

Current route and page files:

```text
src/ui/src/app/app.routes.ts
src/ui/src/app/layout/app-layout/
src/ui/src/app/pages/calendar-events/
src/ui/src/app/shared/components/toolbar/
```

Use the page-first structure in
[`architecture/application-structure.md`](architecture/application-structure.md)
as the UI grows. Keep route-level pages under `pages/`, reusable browser UI
under `shared/`, and persistent application chrome under `layout/`.

## Responsibilities

Use the UI workspace for browser-facing presentation and interaction behavior:

- Routes, layouts, pages, components, forms, and browser state.
- Frontend API client code that calls the Azure Functions REST API.
- Client-side formatting and interaction validation that improves the user
  experience before the backend receives a request.
- Frontend tests for components, routes, services, and user-facing behavior.

Do not place backend scheduling rules, persistence behavior, OAuth token
storage, YouTube API calls, WordPress API calls, or Azure Table Storage access
in the frontend. If frontend code needs those capabilities, add or call a
backend API use case.

## API Access

Frontend API access must be isolated behind explicit services or client
modules instead of being spread through components.

The canonical backend HTTP contracts live in [`../api/http/`](../api/http/).
UI docs should link to those contracts rather than duplicating request and
response shapes.

Deploy-specific public settings should use the runtime configuration approach
defined in
[`architecture/runtime-configuration.md`](architecture/runtime-configuration.md)
when the first UI API client is introduced.
