# Technology Stack

This chapter records selected platform and tooling choices. See
[`overview.md`](overview.md) for project structure, and
[`../development/build-and-test.md`](../development/build-and-test.md) for
commands.

## Runtime

- .NET `net10.0` is the target framework for application, infrastructure,
  Azure Functions, and test projects.
- Projects use SDK-style C# project files with nullable reference types and
  implicit usings enabled.

## API Host

- `YTSkedy.AzureFunctions` hosts the REST API using Azure Functions v4.
- The Functions project uses the isolated worker model with ASP.NET Core HTTP
  integration through `ConfigureFunctionsWebApplication`.
- The implemented endpoint is `POST /api/calendar-events`; its API contract is
  discoverable through the API host OpenAPI surface.
- The current HTTP trigger uses Azure Functions `Function` authorization level.
- HTTP endpoints should stay thin and delegate business behavior to
  `YTSkedy.Scheduling.Application`.
- Application Insights worker telemetry is registered in the Functions host.

## Persistence

- Azure Table Storage is the selected persistence technology for application
  owned scheduling data.
- Persistence code belongs in `YTSkedy.Infrastructure` and implements ports
  defined by `YTSkedy.Scheduling.Application`.
- The initial calendar event creation adapter uses `Azure.Data.Tables`
  version `12.11.0`.
- Calendar events use a table named by `AzureStorage:CalendarEventsTableName`,
  defaulting to `CalendarEvents`.
- The storage connection string is read from `AzureStorage:ConnectionString`,
  then `AzureWebJobsStorage`.
- See [`persistence.md`](persistence.md) for the current table storage behavior.

## External Integrations

- Azure B2C is the planned user authentication provider at the API boundary,
  but it is not implemented yet.
- YouTube access should use adapter modules in `YTSkedy.Infrastructure`; no
  YouTube adapter is implemented yet.
- WordPress access should use adapter modules in `YTSkedy.Infrastructure`; no
  WordPress adapter is implemented yet.
- Domain and application code must not depend directly on external SDKs.

## Unit Testing

- xUnit is the unit testing framework.
- Prefer small hand-written stubs or fakes for simple dependencies.
- Use Moq only when mocking behavior is complex enough that it reduces test
  code complexity.
- Unit tests should avoid real Azure, YouTube, WordPress, network, filesystem,
  and credential dependencies.
- Manual `.http` files under `src/Test/YTSkedy.AzureFunctions.IntegrationTest/`
  are local integration checks, not xUnit tests.

See [`../development/testing.md`](../development/testing.md) for testing
practices.
