# Technology Stack

This chapter records selected platform and tooling choices. See
[`overview.md`](overview.md) for project structure, and
[`../development/build-and-test.md`](../development/build-and-test.md) for
commands.

## Runtime

- Backend projects live under `src/api/`.
- .NET `net10.0` is the target framework for backend application,
  infrastructure, Azure Functions, and test projects.
- Backend projects use SDK-style C# project files with nullable reference types
  and implicit usings enabled.
- Frontend projects live under `src/ui/`.
- The frontend is an Angular workspace managed with npm.

## API Host

- `YTSkedy.AzureFunctions` hosts the REST API under `src/api/` using Azure
  Functions v4.
- The Functions project uses the isolated worker model with ASP.NET Core HTTP
  integration through `ConfigureFunctionsWebApplication`.
- The implemented endpoints are `POST /api/calendar-events` and
  `GET /api/calendar-events?year={year}&month={month}`. The API contract is
  discoverable through the API host OpenAPI surface.
- The current HTTP trigger uses Azure Functions `Function` authorization level.
- HTTP endpoints should stay thin and delegate business behavior to
  `YTSkedy.Scheduling.Application`.
- Application Insights worker telemetry is registered in the Functions host.

## Frontend

- `src/ui/` contains the Angular browser application.
- The frontend package is named `ytskedy-ui`.
- `package.json` declares `npm@11.13.0` as the frontend package manager.
- Angular packages currently use version `22.0.0` ranges.
- TypeScript currently uses `~6.0.2`.
- Styling uses SCSS.
- Routing is configured through Angular router, with no product routes defined
  yet.
- The current UI is the generated Angular shell and does not call the backend
  API yet.
- Future frontend API access should be isolated behind explicit services or
  client modules instead of being spread through components.

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

- xUnit is the backend unit testing framework.
- Vitest with jsdom is the frontend unit testing setup exposed through
  `npm test` in `src/ui/`.
- Prefer small hand-written stubs or fakes for simple backend dependencies.
- Use Moq only when mocking behavior is complex enough that it reduces backend
  test code complexity.
- Unit tests should avoid real Azure, YouTube, WordPress, network, filesystem,
  and credential dependencies unless the test is explicitly an integration
  test.
- Manual `.http` files under
  `src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/`
  are local integration checks, not xUnit tests.

See [`../development/testing.md`](../development/testing.md) for testing
practices.

## Deployment

- The current GitHub Actions deployment workflow builds, tests, publishes, and
  deploys only the backend Azure Functions app.
- No production deployment target or workflow is defined for the Angular
  frontend yet.
