# Calendar Events HTTP Contract

Calendar event endpoints are hosted by `YTSkedy.AzureFunctions` under the
Azure Functions `/api` prefix.

## Authorization

Calendar event HTTP triggers run at `AuthorizationLevel.Anonymous`. The
security boundary is the worker-side bearer-token middleware, not the
Functions host key check.

Every call must:

- Present a Microsoft Entra External ID access token via
  `Authorization: Bearer <token>`. Missing, invalid, expired, wrong-audience,
  or wrong-issuer tokens return `401`.
- Carry the scope required by the endpoint (`CalendarEvents.Read` for `GET`,
  `CalendarEvents.Write` for `POST`, `PUT`, `DELETE`, platform publish, and
  platform-publication delete). Wrong scope returns `403`.
- Carry the `CalendarEvents.Operator` app role in the `roles` claim. Missing
  role returns `403`.

`x-functions-key` is no longer accepted on these endpoints. Local manual
checks acquire a bearer token via the `az`-based recipe documented in
`docs/api/development/build-and-test.md`.

## Create Calendar Event

```text
POST /api/calendar-events
```

Request body:

```json
{
  "start": {
    "localDateTime": "2026-06-06T10:00:00",
    "timeZoneId": "America/Vancouver"
  },
  "descriptions": [
    {
      "language": "ru",
      "title": "Russian stream 1"
    },
    {
      "language": "en",
      "title": "English stream 1",
      "description": "Description for stream 1 in English"
    }
  ]
}
```

Success response:

```json
{
  "calendarEventId": "start-20260606T170000Z-6f9619ff8b864fb5bdfd4f5c2f2f16a1"
}
```

Current error behavior:

- Invalid JSON returns `400 Bad Request` with a plain string message.
- Missing request body returns `400 Bad Request` with a plain string message.

Production release requirements:

- Broader command validation must return stable client-facing errors.
- Storage conflicts must map to stable HTTP responses.
- Unexpected storage failures must avoid leaking provider details or secrets.

## List Calendar Events

```text
GET /api/calendar-events?page={page}&pageSize={pageSize}&sort={sort}&direction={direction}
```

Returns one server-side sorted page of calendar events. The page is computed
over all stored events; sorting, paging, and the total count are applied by the
API.

Query parameters (all optional):

- `page`: zero-based page index. Non-negative integer. Default `0`.
- `pageSize`: page size from `1` through `100`. Default `10`.
- `sort`: sort field, one of `scheduledStart`, `timeZone`, or `title`
  (case-insensitive). Default `scheduledStart`. `scheduledStart` orders by the
  UTC start instant. `title` orders by the English (`en`) description title.
- `direction`: `asc` or `desc` (case-insensitive). Default `desc`.
- `year` and `month`: optional local-calendar-month filter. When supplied they
  must be supplied together; `year` is `1000` through `9999` and `month` is `1`
  through `12`. When omitted, all events are candidates.

With no query parameters the API returns the first page (`page=0`,
`pageSize=10`) sorted by scheduled start descending. Every sort applies the
calendar event id ascending as a deterministic secondary key, so paging stays
stable when the primary field ties. `year`/`month` refer to the event's
submitted local calendar month; the API does not infer the current month,
current year, or machine local time zone.

Success response (`200 OK`) is a paged envelope:

```json
{
  "items": [
    {
      "calendarEventId": "start-20260606T170000Z-6f9619ff8b864fb5bdfd4f5c2f2f16a1",
      "start": {
        "localDateTime": "2026-06-06T10:00:00",
        "timeZoneId": "America/Vancouver"
      },
      "scheduledStartUtc": "2026-06-06T17:00:00+00:00",
      "descriptions": [
        {
          "language": "ru",
          "title": "Russian stream 1",
          "description": null
        },
        {
          "language": "en",
          "title": "English stream 1",
          "description": "Description for stream 1 in English"
        }
      ]
    }
  ],
  "page": 0,
  "pageSize": 10,
  "totalCount": 1,
  "sort": "scheduledStart",
  "direction": "desc"
}
```

- `items` is the requested page of events. `page`, `pageSize`, and the echoed
  `sort`/`direction` reflect the applied query; `totalCount` is the full
  candidate count across all pages and drives the client paginator.
- Each item carries both `start` (the submitted wall-clock `localDateTime` and
  `timeZoneId`) and `scheduledStartUtc`, the same instant as a UTC ISO-8601
  offset string. The UI list renders `scheduledStartUtc`; the create/edit form
  works in local time and zone.
- A page past the end returns `200 OK` with `items` as `[]` and the real
  `totalCount`. No stored events returns `items` as `[]` with `totalCount` `0`.

Current invalid query behavior:

- `page` that is not a non-negative integer returns `400 Bad Request`.
- `pageSize` outside `1` through `100`, or not an integer, returns
  `400 Bad Request`.
- `sort` outside `scheduledStart`, `timeZone`, `title` returns
  `400 Bad Request`.
- `direction` outside `asc`, `desc` returns `400 Bad Request`.
- Supplying only one of `year`/`month` returns `400 Bad Request`; an out-of-range
  `year` or `month` returns `400 Bad Request`.
- A repeated query parameter (more than one value) or an empty value returns
  `400 Bad Request`.
- Error responses currently use specific plain string messages.

## Get Calendar Event

```text
GET /api/calendar-events/{calendarEventId}
```

Returns a single calendar event by id with its per-platform publication state.
Requires the `CalendarEvents.Read` scope. The calendar event fields match one
`items[]` entry from the list endpoint, carrying the wall-clock local start and
time zone (not the UTC instant) so the UI edit form can repopulate from stored
local time. It also carries `scheduledStartUtc`, the same instant as a UTC
ISO-8601 string, which the edit form shows as a read-only translation of the
local start.

Unlike a list item, the details response also carries `platforms`: one entry per
active registered platform with its publish status, plus orphan history rows for
platforms deleted after publishing this event, so a client can render the event
details and its publish state from one read. This is the only endpoint that
exposes per-event publication state; there is no separate event-platform listing
route. The calendar event itself stays provider-neutral; the publish state is
composed at read time and is not stored on the event. The calendar event list
endpoint stays provider-neutral and does not carry `platforms`.

Success response (`200 OK`):

```json
{
  "calendarEventId": "start-20260606T170000Z-6f9619ff8b864fb5bdfd4f5c2f2f16a1",
  "start": {
    "localDateTime": "2026-06-06T10:00:00",
    "timeZoneId": "America/Vancouver"
  },
  "scheduledStartUtc": "2026-06-06T17:00:00+00:00",
  "descriptions": [
    {
      "language": "ru",
      "title": "Russian stream 1",
      "description": null
    },
    {
      "language": "en",
      "title": "English stream 1",
      "description": "Description for stream 1 in English"
    }
  ],
  "platforms": [
    {
      "platformId": "4fb4a32f3f344de1a7c3a9f4a2f94918",
      "platformName": "Main YouTube channel",
      "platformType": "YouTube",
      "status": "NotPublished",
      "externalResourceId": null,
      "publishedUtc": null,
      "platformDeletedUtc": null,
      "canPublish": true,
      "canDeletePublication": false
    }
  ]
}
```

Current behavior:

- Unknown `calendarEventId` returns `404 Not Found`.
- `platforms` is `[]` when no platforms are registered.
- `status` is `NotPublished`, `Publishing`, or `Published`. An active platform
  with no stored publication row is reported as a computed `NotPublished` item;
  no row is created just to read state.
- `externalResourceId` and `publishedUtc` are populated for `Published` items
  and are `null` otherwise.
- `platformDeletedUtc` is set only on orphan history items whose platform was
  deleted. Orphan items carry `canPublish: false` and
  `canDeletePublication: false`.
- `canPublish` is `true` only for an active platform whose publication is
  `NotPublished` and whose calendar event start is future by backend UTC time.
- `canDeletePublication` is `true` only for an active platform whose
  publication is `Published`, has an `externalResourceId`, and whose calendar
  event start is future by backend UTC time.
- The `CalendarEventDetails` edit route (`/calendar-events/{calendarEventId}/edit`)
  consumes this endpoint to load an event into the form.

## Update Calendar Event

```text
PUT /api/calendar-events/{calendarEventId}
```

Replaces the localized descriptions of an existing calendar event in place.
Requires the `CalendarEvents.Write` scope. Only the descriptions can change: the
scheduled start is immutable because changing it would move the event's
scheduling identity and active-start uniqueness key, so the request body carries
no start.

Request body:

```json
{
  "descriptions": [
    {
      "language": "ru",
      "title": "Russian stream 1 (edited)"
    },
    {
      "language": "en",
      "title": "English stream 1 (edited)",
      "description": "Updated description for stream 1 in English"
    }
  ]
}
```

Success response (`200 OK`):

```json
{
  "calendarEventId": "start-20260606T170000Z-6f9619ff8b864fb5bdfd4f5c2f2f16a1"
}
```

The update is an in-place write of the descriptions blob; the event identity and
scheduled start are left unchanged. The list page re-fetches its current page
after a successful edit, so the new descriptions appear in the active sort
order.

Current behavior and error mapping:

- Unknown `calendarEventId` returns `404 Not Found`.
- Invalid JSON returns `400 Bad Request` with a plain string message.
- Missing request body returns `400 Bad Request` with a plain string message.
- The `CalendarEventDetails` edit route (`/calendar-events/{calendarEventId}/edit`)
  consumes this endpoint on save, sending the English and Russian descriptions.

Production release requirements:

- Broader command validation must return stable client-facing errors.
- A concurrent edit that loses the ETag race currently surfaces as `500`; it
  must map to a stable conflict response.

## Delete Calendar Event

```text
DELETE /api/calendar-events/{calendarEventId}
```

Deletes a calendar event that has no platform publication rows. Requires the
`CalendarEvents.Write` scope. Deleting a calendar event is local
application-data cleanup only; this endpoint does not contact YouTube,
WordPress, or any other provider.

Success returns `204 No Content` with an empty body.

Rejected states and error mapping:

- Unknown non-empty `calendarEventId` returns `404 Not Found`. Calendar event
  IDs are opaque; there is no public id-format validation contract.
- Any platform publication row for the event returns `409 Conflict`. Use the
  platform-publication delete route to clean up completed provider publications
  before deleting the event.
- A row that disappears between the existence check and delete write returns
  `204 No Content` because the requested end state already holds.

Scope and proof-of-concept limitations:

- The delete is a hard delete: removed events are not recoverable. Tombstones,
  recycle-bin behavior, audit retention, and restore are out of scope.
- The `CalendarEventDetails` edit route
  (`/calendar-events/{calendarEventId}/edit`) consumes this endpoint from its
  Delete action.

## Platform Publishing

A calendar event is a provider-neutral scheduling record and carries no publish
status of its own. There is no calendar-event-level publish route. Publishing
state lives in platform publications, and publishing always targets an explicit
platform id.

The publication state of an event is part of the calendar event details response
(`GET /api/calendar-events/{calendarEventId}`; see the `platforms` array in
[Get Calendar Event](#get-calendar-event)). The publish action is documented in
[`platforms.md`](platforms.md):

- `POST /api/calendar-events/{calendarEventId}/platforms/{platformId}/publish`
  publishes the event to one selected platform.
- `DELETE /api/calendar-events/{calendarEventId}/platforms/{platformId}/publication`
  deletes one completed provider publication and resets the platform row when
  cleanup succeeds.

## Manual Checks

Manual `.http` checks live under:

```text
src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/CalendarEvents/
```

Before sending local requests:

- Start Azurite or provide an Azure Storage connection string.
- Start the Azure Functions host.
- Select the `local` environment in the `.http` editor.
- Use the host port from the Azure Functions launch profile. The current local
  default is `http://localhost:7087`.

Keep deployed URLs, bearer access tokens, and personal values in
`http-client.env.json.user`, not in tracked environment files. Function keys
no longer apply to these endpoints.
