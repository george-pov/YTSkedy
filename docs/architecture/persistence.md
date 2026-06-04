# Persistence

YTSkedy currently persists calendar event creation data in Azure Table Storage.
The scheduling application defines the `ICalendarEventRepository` port, and
`YTSkedy.Infrastructure` implements it with `AzureCalendarEventRepository`.

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

- Create only. There is no read, update, delete, search, or list API.
- No schema migration or backfill path is defined.
- No explicit retry policy is configured around table writes.
- No production backup or recovery process is defined.
- Calendar events are not yet linked to created YouTube broadcasts or stream
  setup resources.
