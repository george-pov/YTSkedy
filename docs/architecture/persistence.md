# Persistence

YTSkedy currently persists calendar event data in Azure Table Storage from the
backend API under `src/api/`. The scheduling application defines the
`ICalendarEventRepository` and `ICalendarEventReader` ports, and
`YTSkedy.Infrastructure` implements them with `AzureCalendarEventRepository`.

## Configuration

The Azure Functions host creates one `TableClient` for calendar events.

Connection string lookup:

1. `AzureStorage:ConnectionString`
2. `AzureWebJobsStorage`

If neither value is configured, startup fails with:

```text
Azure Table Storage connection string is not configured.
```

Table name lookup:

1. `AzureStorage:CalendarEventsTableName`
2. Default: `CalendarEvents`

For local development, use Azurite with `AzureWebJobsStorage` set to
`UseDevelopmentStorage=true` in the ignored Azure Functions
`local.settings.json` file, or provide a real Azure Storage connection string
outside source control.

## Calendar Event Rows

`AzureCalendarEventRepository.CreateAsync` creates the table if it does not
exist, then inserts one calendar event row.

Entity fields, table keys, and formatting details are defined in code rather
than duplicated in documentation.

`AzureCalendarEventRepository.ListByMonthAsync` reads calendar event rows for a
requested local calendar month and returns application read models ordered by
the mapper.

## Time Zone Handling

The repository converts the scheduled start to UTC before writing the row.

- The submitted local date-time is treated as `DateTimeKind.Unspecified`.
- The submitted time zone ID is resolved with `TimeZoneInfo`.
- IANA IDs are converted to Windows IDs when the runtime supports the
  conversion.
- Skipped local times fail before persistence.
- Repeated local times fail before persistence.

The conversion currently happens in the infrastructure adapter. If the
application starts validating scheduling rules before persistence, keep the API
and storage behavior aligned so the same invalid times are rejected.

## Duplicate Behavior

The current repository implementation allows one calendar event per scheduled
UTC start time. A duplicate insert receives a storage conflict and is raised as:

```text
Calendar event '<calendarEventId>' already exists.
```

The current API does not map that exception to a stable HTTP response yet.

## Current Limits

- Calendar events can be created and listed by local calendar month.
- There is no update, delete, broad search, or pagination API.
- No schema migration or backfill path is defined.
- No explicit retry policy is configured around table writes.
- No production backup or recovery process is defined.
- Calendar events are not yet linked to created YouTube broadcasts or stream
  setup resources.
