# UI Architecture

The Angular frontend lives under `src/ui/`. This document owns frontend
structure, dependency boundaries, shared UI patterns, and API access rules.
Page behavior belongs in [`routes.md`](routes.md).

## Ownership

- Source of truth: Angular workspace structure, frontend responsibilities,
  shared component boundaries, and typed API access patterns.
- Update when: source ownership, shared UI patterns, application boundaries, or
  frontend dependency direction changes.
- Do not duplicate: route behavior or backend request and response contracts.

## Technology Boundary

- The workspace is managed with npm and Angular CLI.
- Angular Material and CDK are available behind app-owned shared components.
- TypeScript and SCSS are the application language and styling boundaries.
- Routed pages render through the `AppLayout` shell.
- Runtime public settings load from `public/config/app-config.json` through an
  app-owned typed configuration boundary.
- Exact tool versions are owned by `src/ui/package.json` and its lockfile.

## Source Layout

```text
src/ui/src/app/
  app.routes.ts
  layout/
    app-layout/
  pages/
    calendar-events/
    calendar-event-details/
    templates/
    platforms/
    settings/
    component-lab/
  shared/
    api/
    auth/
    components/
    config/
    routing/
```

Use the page-first structure in
[`architecture/application-structure.md`](architecture/application-structure.md).
Route orchestration belongs under `pages/`, reusable browser behavior belongs
under `shared/`, and persistent application chrome belongs under `layout/`.

## Page And Shared State

- Route pages own user-visible orchestration, loading state, mutation state,
  errors, confirmation copy, and navigation decisions.
- Form and editor state may be extracted beside its owning page when the state
  has a clear lifecycle and reduces page orchestration complexity.
- Shared route-exit protection lives under `shared/routing/`.
  `pendingChangesGuard` remains copy-free and delegates dirty-state comparison
  and discard wording to the routed page.
- Reusable helpers must not absorb page-specific business rules or copy merely
  to reduce file length.
- Browser state must use backend-computed action eligibility rather than
  re-deriving scheduling or publication policy.

## Shared Components

App-owned Angular Material usage is isolated behind components under
`shared/components/`. Pages compose those components instead of importing
Material primitives directly for the same concern.

`app-data-table` is the shared table boundary. It supports client and server
paging modes, configurable columns, custom cell templates, sorting, paging,
empty states, optional row hover, row activation, and selected-row behavior.
Pages own API sort-field mapping and server refetch behavior.

Shared controls own accessible rendering and generic interaction mechanics.
They do not own route navigation, API calls, domain validation, or page-specific
status copy.

## Responsibilities

The UI workspace owns:

- Routes, layouts, pages, components, forms, and browser interaction state.
- Typed clients and mapping code for the Azure Functions REST API.
- Client-side formatting and interaction validation for responsive feedback.
- Frontend tests for components, routes, clients, and user-visible behavior.

The UI must not own backend scheduling rules, persistence behavior, OAuth token
storage, provider API calls, or Azure Storage access. Those capabilities belong
behind backend use cases.

## API Access

Frontend API access is isolated behind explicit typed services under
`shared/api/`. Page components do not construct transport shapes ad hoc and
route configuration does not perform data mapping.

Canonical backend contracts live in [`../api/http/`](../api/http/). UI docs
describe which contract a page consumes and link to the canonical owner rather
than duplicating request and response shapes.

Deploy-specific public settings use the runtime configuration contract in
[`architecture/runtime-configuration.md`](architecture/runtime-configuration.md).
Deployment behavior is documented in
[`operations/deployment.md`](operations/deployment.md).
