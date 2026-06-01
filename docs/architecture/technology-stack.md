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
- HTTP endpoints should stay thin and delegate business behavior to
  `YTSkedy.Scheduling.Application`.

## Persistence

- Azure Table Storage is the selected persistence technology for application
  owned scheduling data.
- Persistence code belongs in `YTSkedy.Infrastructure` and implements ports
  defined by `YTSkedy.Scheduling.Application`.
- The initial calendar event creation adapter uses `Azure.Data.Tables`.

## External Integrations

- Azure B2C is the planned user authentication provider at the API boundary.
- YouTube access should use adapter modules in `YTSkedy.Infrastructure`.
- WordPress access should use adapter modules in `YTSkedy.Infrastructure`.
- Domain and application code must not depend directly on external SDKs.

## Unit Testing

- xUnit is the unit testing framework.
- Prefer small hand-written stubs or fakes for simple dependencies.
- Use Moq only when mocking behavior is complex enough that it reduces test
  code complexity.
- Unit tests should avoid real Azure, YouTube, WordPress, network, filesystem,
  and credential dependencies.

See [`../development/testing.md`](../development/testing.md) for testing
practices.
