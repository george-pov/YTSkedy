# Durable Docs

Complete inventory for durable YTSkedy documentation. Keep transient plans,
validation notes, local reviews, generated diagnostics, screenshots, and
feature task lists out of this folder unless they become durable product,
architecture, API, UI, development, or operations guidance.

## Ownership

- Source of truth: complete inventory for tracked durable docs under `docs/`
  plus the root documentation entry points.
- Update when: adding, removing, renaming, or moving any durable doc, or when a
  boundary index takes ownership of a new documentation area.
- Validate with: compare this inventory against the output of `rg --files docs`
  plus `README.md` and `CONTEXT.md`, verify edited links resolve, and run
  `git diff --check`.

## Entry Points

- Root developer entrypoint: [`../README.md`](../README.md)
- Project context: [`../CONTEXT.md`](../CONTEXT.md)
- Durable docs inventory: [`README.md`](README.md)
- Shared architecture overview: [`architecture/overview.md`](architecture/overview.md)
- Cross-boundary integration contracts:
  [`architecture/integration-contracts.md`](architecture/integration-contracts.md)
- Domain vocabulary and naming guidance:
  [`development/naming-guidance.md`](development/naming-guidance.md)
- API docs index: [`api/README.md`](api/README.md)
- UI docs index: [`ui/README.md`](ui/README.md)

## Shared Architecture

- Architecture overview: [`architecture/overview.md`](architecture/overview.md)
- Integration contracts:
  [`architecture/integration-contracts.md`](architecture/integration-contracts.md)
- Naming guidance:
  [`development/naming-guidance.md`](development/naming-guidance.md)

## API

- API docs index: [`api/README.md`](api/README.md)
- API architecture: [`api/architecture.md`](api/architecture.md)
- API persistence: [`api/persistence.md`](api/persistence.md)
- API configuration: [`api/configuration.md`](api/configuration.md)

### API HTTP Contracts

- HTTP contracts index: [`api/http/README.md`](api/http/README.md)
- Calendar events HTTP contract:
  [`api/http/calendar-events.md`](api/http/calendar-events.md)
- Platform publishing HTTP contract:
  [`api/http/platforms.md`](api/http/platforms.md)
- Templates HTTP contract:
  [`api/http/templates.md`](api/http/templates.md)

### API Development

- API build and test commands:
  [`api/development/build-and-test.md`](api/development/build-and-test.md)
- API testing guidance:
  [`api/development/testing.md`](api/development/testing.md)

### API Operations

- API deployment:
  [`api/operations/deployment.md`](api/operations/deployment.md)
- YouTube publish setup:
  [`api/operations/youtube-publish-setup.md`](api/operations/youtube-publish-setup.md)
- Platform publication cleanup:
  [`api/operations/platform-publication-cleanup.md`](api/operations/platform-publication-cleanup.md)

## UI

- UI docs index: [`ui/README.md`](ui/README.md)
- UI architecture: [`ui/architecture.md`](ui/architecture.md)
- UI routes: [`ui/routes.md`](ui/routes.md)

### UI Architecture

- Application structure:
  [`ui/architecture/application-structure.md`](ui/architecture/application-structure.md)
- Runtime configuration:
  [`ui/architecture/runtime-configuration.md`](ui/architecture/runtime-configuration.md)

### UI Development

- UI development guidelines:
  [`ui/development/development-guidelines.md`](ui/development/development-guidelines.md)
- UI naming conventions:
  [`ui/development/naming-conventions.md`](ui/development/naming-conventions.md)
- UI styling: [`ui/development/styling.md`](ui/development/styling.md)
- UI responsive layout:
  [`ui/development/responsive-layout.md`](ui/development/responsive-layout.md)
- UI build and test commands:
  [`ui/development/build-and-test.md`](ui/development/build-and-test.md)
- UI testing guidance:
  [`ui/development/testing.md`](ui/development/testing.md)
- UI unit testing:
  [`ui/development/unit-testing.md`](ui/development/unit-testing.md)
- UI end-to-end testing:
  [`ui/development/end-to-end-testing.md`](ui/development/end-to-end-testing.md)
