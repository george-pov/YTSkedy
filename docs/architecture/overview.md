# Architecture Overview

YTSkedy is now split into a backend API workspace and a frontend UI workspace.
The backend uses a small Clean Architecture structure with ports and adapters.
The scheduling core stays free of Angular, Azure Functions, Azure Table
Storage, YouTube, WordPress, and authentication details. Inbound and outbound
adapters depend inward on the scheduling projects. See
[`technology-stack.md`](technology-stack.md) for selected platform and tooling
choices.

## Repository Layout

- `src/api/`: .NET backend solution. Contains the Azure Functions host,
  scheduling domain and application projects, infrastructure adapters, backend
  xUnit tests, and manual `.http` checks.
- `src/ui/`: Angular frontend workspace. Contains the browser application,
  Angular routing and component source, frontend tests, and npm package
  metadata.
- `docs/`: durable architecture, development, deployment, testing, and naming
  guidance.
- `.work/`: local-only workflow records for agents. Durable docs should not
  reference `.work/`.

## Runtime Pattern

Runtime work should follow the same inward dependency shape regardless of the
specific workflow:

```text
Browser UI, HTTP endpoint, or other inbound adapter
    -> request DTO or transport input model
    -> application command or query
    -> application handler
    -> application port interface, when external data or services are needed
    -> infrastructure adapter
    -> external service, database, or storage system
```

Inbound adapters translate transport details into application requests.
Application handlers coordinate one use case. Domain types express scheduling
concepts and rules. Infrastructure adapters perform external work behind
application-owned interfaces.

The Angular UI should call the backend through explicit HTTP API client code
once product workflows are implemented. It may perform client-side interaction
validation for responsiveness, but durable scheduling rules and external side
effects belong in the backend. The REST API contract is discoverable through
the API host OpenAPI surface. See [`persistence.md`](persistence.md) for the
table storage architecture notes.

## Backend Projects

Backend projects live under `src/api/`.

### `YTSkedy.Scheduling.Domain`

Use this project for scheduling concepts and rules that should be true no
matter how the application is hosted or persisted.

Place functionality here when it is:

- A domain entity, value object, or domain-specific state transition.
- A scheduling rule that is independent of HTTP, Azure, YouTube, WordPress, or
  storage mechanics.
- Validation that protects the meaning of a domain concept rather than the
  shape of a request.
- Domain terminology that other projects should share.

Do not place request DTOs, response DTOs, table entities, API clients,
configuration readers, logging adapters, or persistence-specific behavior here.
This project must not depend on Azure, YouTube, WordPress, HTTP, persistence, or
authentication packages.

### `YTSkedy.Scheduling.Application`

Use this project for application use cases and ports. A use case should describe
what the application is trying to do without knowing which host or external
system performs the work.

Place functionality here when it is:

- A command or query accepted by a use case.
- A handler that coordinates domain types and external ports.
- A result type or read model returned by a use case.
- An interface for persistence, YouTube, WordPress, credential storage, clock
  access, or other external systems.
- Use case validation that belongs before calling infrastructure or external
  APIs.

Common type patterns:

- Write use cases: `VerbThingCommand`, `VerbThingHandler`, and
  `VerbThingResult`.
- Read use cases: `GetThingQuery`, `ListThingsQuery`, `SearchThingsQuery`,
  `GetThingHandler`, `ListThingsHandler`, or `SearchThingsHandler`.
- Read results: `ThingDetails`, `ThingSummary`, or a workflow-specific result
  record when the response is not a domain entity.
- External ports: `IThingRepository`, `IYouTubeBroadcastClient`,
  `ICredentialStore`, or another interface named for the application need.

This project depends on `YTSkedy.Scheduling.Domain`. It defines interfaces for
infrastructure adapters but does not implement Azure Table Storage, YouTube, or
WordPress access.

### `YTSkedy.Infrastructure`

Use this project for concrete adapters that satisfy application port
interfaces.

Place functionality here when it is:

- An Azure Table Storage repository implementation.
- A YouTube, WordPress, authentication, credential, clock, telemetry, or other
  external service adapter.
- Mapping between infrastructure-specific data shapes and application or domain
  types.
- Retry, paging, continuation token, ETag, rate limit, or protocol behavior
  required by an external system.
- Storage entity definitions and serialization details.

This project depends inward on `YTSkedy.Scheduling.Application` and
`YTSkedy.Scheduling.Domain`. It should not define application use cases or HTTP
contracts.

### `YTSkedy.AzureFunctions`

Use this project for HTTP-triggered Azure Functions and host composition.

Place functionality here when it is:

- HTTP-triggered endpoints.
- Request and response DTOs for the REST contract.
- Query string, route, header, and request body parsing.
- HTTP status code mapping.
- Dependency injection composition and runtime configuration wiring.
- Authentication and authorization checks that belong at the API boundary.
- Calling one application handler per endpoint.

This project should stay thin. Business rules belong in the scheduling
projects, and external system details belong in `YTSkedy.Infrastructure`.

## Frontend Workspace

The Angular frontend lives under `src/ui/`.

Use this workspace for browser-facing presentation and interaction behavior:

- Routes, layouts, pages, components, forms, and browser state.
- Frontend API client code that calls the Azure Functions REST API.
- Client-side formatting and interaction validation that improves the user
  experience before the backend receives a request.
- Frontend tests for components, routes, services, and user-facing behavior.

Do not place backend scheduling rules, persistence behavior, OAuth token
storage, YouTube API calls, WordPress API calls, or Azure Table Storage access
in the frontend. If frontend code needs those capabilities, add or call a
backend API use case.

## Adding Functionality

When adding a new backend behavior, decide placement by walking inward from the
caller:

1. Define the inbound contract in the host project.
   Add or update an Azure Function, request DTOs, response DTOs, and HTTP error
   mapping in `YTSkedy.AzureFunctions`.
2. Define the use case in the application project.
   Add a command or query, a handler, and a result or read model in
   `YTSkedy.Scheduling.Application`.
3. Add domain types only when the behavior introduces durable scheduling
   concepts or rules.
   Keep pure data transfer and transport validation out of
   `YTSkedy.Scheduling.Domain`.
4. Add or extend application ports when the use case needs data or external
   effects.
   The interface belongs in `YTSkedy.Scheduling.Application`, even when the
   implementation will call Azure, YouTube, WordPress, or another service.
5. Implement ports in `YTSkedy.Infrastructure`.
   Keep storage entities, API client DTOs, serialization, continuation tokens,
   and provider-specific behavior inside the adapter.
6. Wire dependencies in the host project.
   Register handlers, ports, adapters, clients, and configuration in
   `YTSkedy.AzureFunctions`.
7. Validate at the smallest useful level.
   Prefer application handler unit tests with fake ports for use case behavior.
   Add adapter tests when storage or external protocol behavior is important.
   Add HTTP checks for manually exercising the REST contract.

When adding a frontend-backed workflow, define the browser route, component,
form state, and API client behavior in `src/ui/`, then add or extend backend
API endpoints only where the UI needs durable scheduling behavior or external
effects. Keep request and response DTOs stable enough for the UI to consume,
and update tests on both sides when that contract changes.

## Placement Checklist

Use these questions before creating a new class or interface:

- Does it model scheduling language or enforce a rule independent of delivery
  and persistence? Put it in `YTSkedy.Scheduling.Domain`.
- Does it coordinate a user or system action? Put the command or query,
  handler, result, and required port interface in
  `YTSkedy.Scheduling.Application`.
- Does it call a database, storage service, external API, file system, clock,
  logger, or credential provider? Put the implementation in
  `YTSkedy.Infrastructure` behind an application port.
- Does it describe HTTP input, HTTP output, route shape, authorization,
  dependency injection, or host configuration? Put it in
  `YTSkedy.AzureFunctions`.
- Does it describe browser presentation, a route, component state, a form, or
  frontend API call orchestration? Put it in `src/ui/`.
- Would placing the type require an inward project to reference Azure
  Functions, Azure Storage, YouTube, WordPress, HTTP, or authentication
  packages? Move it outward.
- Is the type named after a technical provider instead of an application need?
  Keep provider-specific names in infrastructure and expose application-focused
  names through ports.

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
