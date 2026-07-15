# API Architecture

The backend API lives under `src/api/`. It uses a small Clean Architecture
structure with ports and adapters. Inbound and outbound adapters depend inward
on the scheduling projects.

## Projects

### `YTSkedy.Scheduling.Domain`

Use this project for scheduling concepts and rules that should be true no
matter how the application is hosted or persisted.

Place functionality here when it is:

- A domain entity, value object, or domain-specific state transition.
- A scheduling rule independent of HTTP, Azure, YouTube, WordPress, or storage
  mechanics.
- Validation that protects the meaning of a domain concept rather than the
  shape of a request.
- Domain terminology that backend projects should share.

Do not place request DTOs, response DTOs, table entities, API clients,
configuration readers, logging adapters, or persistence-specific behavior here.

### `YTSkedy.Scheduling.Application`

Use this project for application use cases and ports.

Place functionality here when it is:

- A command or query accepted by a use case.
- A handler that coordinates domain types and external ports.
- A result type or read model returned by a use case.
- An interface for persistence, YouTube, WordPress, credential storage, clock
  access, or another external system.
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
- Mapping between infrastructure-specific data shapes and application or
  domain types.
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

## Adding API Behavior

When adding a backend behavior, decide placement by walking inward from the
caller:

1. Define the inbound contract in the host project. Add or update an Azure
   Function, request DTOs, response DTOs, and HTTP error mapping in
   `YTSkedy.AzureFunctions`.
2. Define the use case in the application project. Add a command or query, a
   handler, and a result or read model in
   `YTSkedy.Scheduling.Application`.
3. Add domain types only when the behavior introduces durable scheduling
   concepts or rules.
4. Add or extend application ports when the use case needs data or external
   effects.
5. Implement ports in `YTSkedy.Infrastructure`.
6. Wire dependencies in the host project.
7. Validate at the smallest useful level.

## Provider Publication Lifecycle

Provider publication is coordinated by application handlers and implemented by
infrastructure adapters. The authoritative row for one calendar event and one
platform uses these states:

- A missing row is projected as `NotPublished`.
- `Publishing` is a transient conditional-write guard while one provider
  request is active.
- `Published` means the required provider operation and local finalization
  succeeded.
- `Failed` is an operator-visible caught failure. It can retain a provider
  resource id and can be conditionally retried after the operator verifies the
  provider.

Provider adapters report a known external id when a later required step fails.
YouTube checkpoints the broadcast id immediately after insert, before optional
video metadata work. WordPress checkpoints its validated post id before
returning success. The application records handled started failures as `Failed`
and does not automatically retry or delete provider resources.

Reads and publish preflight honor the HTTP request token. Immediately before
`StartPublishingAsync`, the handler checks that token once and switches to a
server-owned operation token bounded by the publication deadline and host
shutdown. Every final-state write receives a fresh short deadline. A client
disconnect after start therefore does not own the provider attempt. Hard
process termination can still interrupt finalization and leave `Publishing`.
An authenticated operator can recover an eligible stale row to `Failed` through
an exact timestamp and ETag conditional write after verifying the provider.

For YouTube, the infrastructure adapter owns the multi-step contract. It
creates the scheduled broadcast privately, performs a conditional
preservation read only when a video update is required, copies mutable values
for the included parts, applies category, altered or synthetic disclosure,
made-for-kids, and final privacy, then reports success. Google SDK request and
response types remain in `YTSkedy.Infrastructure`.

## Placement Checklist

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
- Would placing the type require an inward project to reference Azure
  Functions, Azure Storage, YouTube, WordPress, HTTP, or authentication
  packages? Move it outward.
- Is the type named after a technical provider instead of an application need?
  Keep provider-specific names in infrastructure and expose
  application-focused names through ports.

## Selected Technology

- .NET `net10.0` is the target framework for backend application,
  infrastructure, Azure Functions, and test projects.
- `YTSkedy.AzureFunctions` hosts the REST API using Azure Functions v4.
- The Functions project uses the isolated worker model with ASP.NET Core HTTP
  integration through `ConfigureFunctionsWebApplication`.
- The current HTTP trigger uses Azure Functions `Function` authorization
  level.
- Application Insights worker telemetry is registered in the Functions host.
- Azure Table Storage is the selected persistence technology for
  application-owned scheduling data.
- xUnit is the backend unit testing framework.
