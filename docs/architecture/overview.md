# Architecture Overview

YTSkedy is an open source full-stack application split into a backend API
workspace and a frontend UI workspace. This document owns the shared system map
only. Boundary-specific implementation guidance lives in the API and UI docs.

## Repository Layout

- `src/api/`: .NET backend solution. Contains the Azure Functions host,
  scheduling domain and application projects, infrastructure adapters, backend
  xUnit tests, and manual `.http` checks.
- `src/ui/`: Angular frontend workspace. Contains browser application source,
  Angular routing and component source, frontend tests, and npm package
  metadata.
- `docs/api/`: durable API architecture, HTTP contracts, persistence,
  configuration, development, testing, and operations docs.
- `docs/ui/`: durable UI architecture, routes, development, testing, and
  operations docs.
- `docs/architecture/`: cross-boundary system architecture and integration
  contract docs.
- `docs/development/naming-guidance.md`: shared domain vocabulary and naming
  conventions.
- `.work/`: local-only workflow records for agents. Durable docs should not
  reference `.work/`.

## System Responsibilities

- YTSkedy owns production-grade local application flow, validation, templates,
  scheduling rules, integration orchestration, and contributor-facing
  documentation.
- The Angular UI owns browser presentation, route-level interaction state, and
  user input collection.
- The backend API owns durable scheduling behavior, persistence, server-side
  validation, external integration orchestration, and API contract
  enforcement.
- Azure Table Storage stores application-owned calendar event rows.
- YouTube owns channel identity, live broadcast resources, live stream
  resources, visibility behavior, and API enforcement.
- OAuth providers own authorization flows and token issuance. Credential
  material must not be committed to the repository.

## Runtime Shape

Runtime work should keep external and host concerns outside the scheduling
core:

```text
Browser UI
    -> Azure Functions REST API
    -> application command or query
    -> application handler
    -> application port interface, when external data or services are needed
    -> infrastructure adapter
    -> external service, database, or storage system
```

The UI may perform client-side interaction validation for responsiveness, but
durable scheduling rules and externally visible side effects belong in the API.
The API exposes transport contracts and translates them into application use
cases. Infrastructure performs external work behind application-owned ports.

## Dependency Direction

```text
src/ui Angular application
    -> Azure Functions REST API

YTSkedy.AzureFunctions
    -> YTSkedy.Scheduling.Application
        -> YTSkedy.Scheduling.Domain

YTSkedy.Infrastructure
    -> YTSkedy.Scheduling.Application
    -> YTSkedy.Scheduling.Domain
```

Domain and application code must not depend on Angular, Azure Functions, Azure
Table Storage, YouTube, WordPress, authentication frameworks, or browser APIs.

## Documentation Ownership

Use the boundary docs for implementation detail:

- API architecture: [`../api/architecture.md`](../api/architecture.md)
- API HTTP contracts: [`../api/http/`](../api/http/)
- API persistence: [`../api/persistence.md`](../api/persistence.md)
- API configuration: [`../api/configuration.md`](../api/configuration.md)
- UI architecture: [`../ui/architecture.md`](../ui/architecture.md)
- UI routes: [`../ui/routes.md`](../ui/routes.md)
- Cross-boundary contracts:
  [`integration-contracts.md`](integration-contracts.md)

Do not duplicate endpoint request or response shapes in UI docs. Link to the
API HTTP contract instead. Do not duplicate domain vocabulary outside
[`../development/naming-guidance.md`](../development/naming-guidance.md).
