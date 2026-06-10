# YTSkedy

YTSkedy is an open source application for production-grade scheduling
automation of YouTube streams.

## Purpose

This README is the human developer table of contents for durable docs.

## Repository Layout

- `src/api/`: .NET Azure Functions backend API, scheduling domain and
  application projects, infrastructure adapters, backend tests, and manual
  `.http` checks.
- `src/ui/`: Angular frontend workspace, browser application source, frontend
  tests, and npm package metadata.
- `docs/`: durable shared architecture, API, UI, development, and operations
  guidance.
- `.work/`: local-only agent workflow records and transient planning support.

## Chapters

### Shared

- Project context: [`CONTEXT.md`](CONTEXT.md)
- Shared architecture overview: [`docs/architecture/overview.md`](docs/architecture/overview.md)
- Cross-boundary integration contracts: [`docs/architecture/integration-contracts.md`](docs/architecture/integration-contracts.md)
- Domain vocabulary and naming guidance: [`docs/development/naming-guidance.md`](docs/development/naming-guidance.md)

### API

- Index: [`docs/api/README.md`](docs/api/README.md)
- API architecture: [`docs/api/architecture.md`](docs/api/architecture.md)
- API HTTP contracts: [`docs/api/http/`](docs/api/http/)
- API persistence: [`docs/api/persistence.md`](docs/api/persistence.md)
- API configuration: [`docs/api/configuration.md`](docs/api/configuration.md)
- API build and test commands: [`docs/api/development/build-and-test.md`](docs/api/development/build-and-test.md)
- API deployment: [`docs/api/operations/deployment.md`](docs/api/operations/deployment.md)

### UI

- Index: [`docs/ui/README.md`](docs/ui/README.md)
- UI architecture: [`docs/ui/architecture.md`](docs/ui/architecture.md)
- UI application structure: [`docs/ui/architecture/application-structure.md`](docs/ui/architecture/application-structure.md)
- UI runtime configuration: [`docs/ui/architecture/runtime-configuration.md`](docs/ui/architecture/runtime-configuration.md)
- UI routes: [`docs/ui/routes.md`](docs/ui/routes.md)
- UI development guidelines: [`docs/ui/development/development-guidelines.md`](docs/ui/development/development-guidelines.md)
- UI naming conventions: [`docs/ui/development/naming-conventions.md`](docs/ui/development/naming-conventions.md)
- UI styling: [`docs/ui/development/styling.md`](docs/ui/development/styling.md)
- UI responsive layout: [`docs/ui/development/responsive-layout.md`](docs/ui/development/responsive-layout.md)
- UI build and test commands: [`docs/ui/development/build-and-test.md`](docs/ui/development/build-and-test.md)
- UI unit testing: [`docs/ui/development/unit-testing.md`](docs/ui/development/unit-testing.md)
- UI end-to-end testing: [`docs/ui/development/end-to-end-testing.md`](docs/ui/development/end-to-end-testing.md)
