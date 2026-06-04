# UI Application Structure

Current application structure guidance for the YTSkedy Angular frontend.

## Direction

Use modern standalone Angular patterns:

- standalone components
- route-level lazy loading when a route has enough code to justify it
- root providers in `ApplicationConfig`
- signals for user input and local UI state
- RxJS for HTTP, debounce, cancellation, search, and complex async chains

Do not introduce Angular NgModules for new application code unless a
compatibility requirement forces it.

## Root Structure

Keep the app page-first and shallow. The current source layout is:

```text
src/ui/src/app/
  app.config.ts
  app.html
  app.routes.ts
  app.routes.spec.ts
  app.ts

  layout/
    app-layout/

  pages/
    calendar-events/
```

As the UI grows, use this target structure:

```text
src/ui/src/app/
  layout/
  pages/
  shared/
```

Only create folders when code exists. Do not add empty structure only to match
the target tree.

## Layout

Use `layout/` only for the app layout and persistent chrome.

```text
layout/
  app-layout/
```

`app-layout` should own the routed application frame and the `router-outlet`
once the app needs shared chrome. Additional persistent chrome belongs under
`layout/` only when it is shared across routed pages.

The current root `App` renders the Angular router outlet. The route table then
uses `AppLayout` as the routed shell so page routes render through the
application chrome.

## Pages

Use `pages/` for route-level screens and page-owned flows. YTSkedy is the
application, not a folder under `features/`.

Current page structure:

```text
pages/
  calendar-events/
```

Keep page flows flat first. Add page-local `components/`, `sections/`,
`services/`, or `models/` only when there are enough local files to justify
the grouping.

Calendar event API clients and display mapping should start near the
`calendar-events` page flow. Move them to `shared/` only when another page
actually reuses them.

## Shared

Use `shared/` for reusable application code that is not owned by one page flow.

Allowed shared folders when code exists:

```text
shared/
  components/
  config/
  models/
  services/
  utils/
```

Keep scheduling workflow decisions and page-flow branching out of generic
shared components.

Create `shared/utils/` only for small, behavior-specific pure functions. File
names inside it must describe the behavior they own instead of using generic
names such as `utils.ts` or `helpers.ts`.

`shared/config/` should own runtime configuration loading and injection when a
deploy-specific UI setting is introduced. Page-flow services should consume
runtime settings through that shared config boundary instead of owning
environment-specific constants.

## Assets

Use `src/ui/public/` for static runtime assets served as-is, such as images,
icons, favicons, config files, and self-hosted fonts. Prefer
`src/ui/public/assets/...` over `src/ui/src/assets/` unless an asset needs
build processing.

## Routing

Keep root routes thin:

- route current root behavior through `app.routes.ts`
- prefer `loadComponent` for route-level standalone pages once page size
  warrants lazy loading
- keep route guards near the page flow that owns them
- route `**` to a not-found page after one exists
- redirect wildcard routes only while no not-found page exists

Current routes are documented in [`../routes.md`](../routes.md).
