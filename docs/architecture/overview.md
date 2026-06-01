# Architecture Overview

YTSkedy uses a small Clean Architecture structure with ports and adapters. The
scheduling core stays free of Azure Functions, Azure Table Storage, YouTube,
WordPress, and authentication details. Inbound and outbound adapters depend
inward on the scheduling projects. See
[`technology-stack.md`](technology-stack.md) for selected platform and tooling
choices.

## Current Runtime Flow

The current implemented flow is initial calendar event creation:

```text
POST /api/calendar-events
    -> YTSkedy.AzureFunctions request DTO
    -> CreateEventCommand
    -> CreateEventHandler
    -> ICalendarEventRepository
    -> AzureCalendarEventRepository
    -> Azure Table Storage
```

The API contract is discoverable through the API host OpenAPI surface. See
[`persistence.md`](persistence.md) for the table storage architecture notes.

## Projects

### `YTSkedy.Scheduling.Domain`

Pure scheduling domain model and rules.

Current scope:

- Calendar event concepts.
- Scheduled start time and time zone rules.
- Description content needed to generate YouTube stream metadata.

Planned scope:

- Scheduled stream concepts.
- Stream template concepts.
- Scheduling rules that can be enforced before external resources are touched.

This project must not depend on Azure, YouTube, WordPress, HTTP, persistence,
or authentication packages.

### `YTSkedy.Scheduling.Application`

Application use cases and orchestration for scheduling workflows.

Current scope:

- Defining ports for persistence and external systems.
- Creating calendar events through `CreateEventHandler`.

Planned scope:

- Creating and updating scheduling plans.
- Coordinating calendar event input with scheduled stream creation.
- Validation at use case boundaries before external resources are touched.

This project depends on `YTSkedy.Scheduling.Domain`. It defines interfaces for
infrastructure adapters but does not implement Azure Table Storage, YouTube, or
WordPress access.

### `YTSkedy.Infrastructure`

Concrete adapters for external systems.

Current scope:

- Azure Table Storage persistence.

Planned scope:

- YouTube API access.
- WordPress API access.
- Azure B2C integration support.
- Configuration, telemetry, and other runtime adapters.

This project depends inward on `YTSkedy.Scheduling.Application` and
`YTSkedy.Scheduling.Domain`.

### `YTSkedy.AzureFunctions`

REST API host and Azure Functions entry point.

Current scope:

- HTTP-triggered endpoints.
- Request and response DTOs.
- Dependency injection composition.
- Calling application use cases.

Planned scope:

- Authentication and authorization integration at the API boundary beyond the
  current Azure Functions function-key authorization level.

This project should stay thin. Business rules belong in the scheduling
projects, and external system details belong in `YTSkedy.Infrastructure`.

## Dependency Direction

```text
YTSkedy.AzureFunctions
    -> YTSkedy.Scheduling.Application
        -> YTSkedy.Scheduling.Domain

YTSkedy.Infrastructure
    -> YTSkedy.Scheduling.Application
    -> YTSkedy.Scheduling.Domain
```
