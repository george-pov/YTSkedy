# Architecture Overview

YTSkedy is an open source full-stack application split into a backend API
workspace and a frontend UI workspace. This document owns the shared system map
only. Boundary-specific implementation guidance lives in the API and UI docs.

## Ownership

- Source of truth: shared system map, responsibility boundaries, runtime shape,
  dependency direction, and durable documentation ownership rules.
- Update when: repository boundaries, system responsibilities, runtime flow,
  dependency direction, or documentation ownership changes.
- Validate with: verify links to boundary docs, confirm endpoint shapes stay in
  API HTTP docs, confirm domain vocabulary stays in naming guidance, and run
  `git diff --check`.

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
- `docs/operations/`: cross-boundary deployment and environment operations.
- `docs/development/domain-vocabulary.md`: shared domain vocabulary.
- `docs/development/naming-guidance.md`: shared identifier naming rules.
- `bicep/`: one subscription-scope Azure entry point, dev and prod parameter
  files, and shared resource-group modules.
- `scripts/azure/`: guarded name, validation, what-if, and deployment commands
  for the tracked Azure environments.

## System Responsibilities

- YTSkedy owns production-grade local application flow, validation, templates,
  scheduling rules, integration orchestration, and contributor-facing
  documentation.
- The Angular UI owns browser presentation, route-level interaction state, and
  user input collection.
- The backend API owns durable scheduling behavior, persistence, server-side
  validation, external integration orchestration, and API contract
  enforcement.
- Azure Table Storage stores application-owned calendar event, template,
  platform, and platform-publication rows.
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

## Deployed Environment Shape

The application has separate Azure `dev` and `prod` environments built from one
shared Bicep shape. Each environment owns its resource group, Function host and
deployment storage, application data storage, UI storage, monitoring,
deployment identity, OIDC subject, role scopes, Entra registrations, runtime
configuration, CORS, and data.

Pushes to `main` deploy application code to dev. Prod promotion is manual,
targets the protected `prod` GitHub Environment, and uses a commit already
validated in dev. Deployment identities have only resource-scoped Function and
UI permissions. Prod has a resource-group delete lock after validation.

The reusable environment model, deployment inputs, manual Entra and CORS
boundaries, validation, and recovery process are documented in
[`../operations/azure-environments.md`](../operations/azure-environments.md).

## Documentation Ownership

Use the boundary docs for implementation detail:

- API architecture: [`../api/architecture.md`](../api/architecture.md)
- API HTTP contracts: [`../api/http/`](../api/http/)
- API persistence: [`../api/persistence.md`](../api/persistence.md)
- API configuration: [`../api/configuration.md`](../api/configuration.md)
- UI architecture: [`../ui/architecture.md`](../ui/architecture.md)
- UI routes: [`../ui/routes.md`](../ui/routes.md)
- UI deployment: [`../ui/operations/deployment.md`](../ui/operations/deployment.md)
- Azure environments:
  [`../operations/azure-environments.md`](../operations/azure-environments.md)
- Cross-boundary contracts:
  [`integration-contracts.md`](integration-contracts.md)

Do not duplicate endpoint request or response shapes in UI docs. Link to the
API HTTP contract instead. Do not duplicate domain vocabulary outside
[`../development/domain-vocabulary.md`](../development/domain-vocabulary.md).
