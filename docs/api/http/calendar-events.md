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

Function keys are not part of authorization for these endpoints.

## Create Calendar Event

```text
POST /api/calendar-events
```

Create reads the current event text fields setting, validates the submitted
values against it, and stores a complete text snapshot on the calendar event.
All configured fields are required.

Request body:

```json
{
  "start": {
    "localDateTime": "2026-06-06T10:00:00",
    "timeZoneId": "America/Vancouver"
  },
  "texts": [
    {
      "fieldKey": "text1",
      "value": "Saturday stream"
    },
    {
      "fieldKey": "text2",
      "value": "Description for Saturday stream"
    }
  ]
}
```

Success response:

```json
{
  "calendarEventId": "6f9619ff8b864fb5bdfd4f5c2f2f16a1"
}
```

`calendarEventId` is an opaque lowercase `guid:N` string. Other id shapes are
unsupported and return not found.

Current error behavior:

- Invalid JSON returns `400 Bad Request` with a plain string message.
- Missing request body returns `400 Bad Request` with a plain string message.
- Missing, unknown, duplicate, blank, or over-length text values return
  `400 Bad Request`.
- Detected duplicate scheduled starts return `409 Conflict`. Duplicate
  detection is best-effort for normal sequential writes; concurrent duplicate
  writes are an accepted risk.

Current limitations:

- Broader command validation does not return stable structured error bodies.
- Unexpected storage failures are not normalized beyond the documented endpoint
  mappings.

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
- `sort`: sort field, one of `scheduledStart`, `timeZone`, `title`, or
  `publicationStatus`
  (case-insensitive). Default `scheduledStart`. `scheduledStart` orders by the
  UTC start instant. `title` orders by `displayTitle`. `publicationStatus`
  orders by aggregate lifecycle state: `NotPublished`, `PartiallyPublished`,
  `FullyPublished`, then `Failed`.
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
      "calendarEventId": "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
      "start": {
        "localDateTime": "2026-06-06T10:00:00",
        "timeZoneId": "America/Vancouver"
      },
      "scheduledStartUtc": "2026-06-06T17:00:00+00:00",
      "displayTitle": "Saturday stream",
      "publicationStatus": "PartiallyPublished",
      "texts": [
        {
          "fieldKey": "text1",
          "label": "Title",
          "type": "ShortText",
          "maxLength": 50,
          "value": "Saturday stream"
        },
        {
          "fieldKey": "text2",
          "label": "Description",
          "type": "LongText",
          "maxLength": 2500,
          "value": "Description for Saturday stream"
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
  offset string. The UI list renders the submitted local date-time and time-zone
  id while its scheduled-start sort continues to use `scheduledStartUtc`; the
  create/edit form works in local time and zone.
- `displayTitle` is the backend-defined representative title for list display
  and `title` sorting. It is the first `ShortText` value in the stored event
  snapshot, falling back to the first text value when the snapshot has no short
  text field.
- `publicationStatus` is a required informational aggregate with values
  `NotPublished`, `PartiallyPublished`, `FullyPublished`, or `Failed`. It is a
  supported list sort field but does not control any write action.
  - `NotPublished` means the event's derived published-platform-id set is empty.
  - `FullyPublished` means at least one active platform exists and every active
    platform id is in the derived set.
  - Any other non-empty derived set is `PartiallyPublished`, including a set
    containing only ids for deleted platforms when no active platform exists.
  - `Failed` is reserved by the contract. The current publication lifecycle
    does not capture or persist an aggregate failure state.
- Aggregation compares each event with the platforms active at list-read time.
  Adding or deleting a platform can therefore reclassify past and future list
  rows. Per-platform publication rows remain authoritative and are not returned
  by this endpoint.
- `texts` is the event's stored snapshot. It carries enough field metadata for
  clients to display or edit the event without consulting the current setting.
- A page past the end returns `200 OK` with `items` as `[]` and the real
  `totalCount`. No stored events returns `items` as `[]` with `totalCount` `0`.

Current invalid query behavior:

- `page` that is not a non-negative integer returns `400 Bad Request`.
- `pageSize` outside `1` through `100`, or not an integer, returns
  `400 Bad Request`.
- `sort` outside `scheduledStart`, `timeZone`, `title`, `publicationStatus`
  returns
  `400 Bad Request`.
- `direction` outside `asc`, `desc` returns `400 Bad Request`.
- Supplying only one of `year`/`month` returns `400 Bad Request`; an
  out-of-range `year` or `month` returns `400 Bad Request`.
- A repeated query parameter (more than one value) or an empty value returns
  `400 Bad Request`.
- Error responses currently use specific plain string messages.

## Get New Calendar Event Start Suggestion

```text
GET /api/calendar-events/start-suggestion?fallbackTimeZoneId={ianaId}
```

Requires `CalendarEvents.Read`. The optional `fallbackTimeZoneId` must occur
once, be non-empty, and identify a recognized time zone. Invalid values return
`400 Bad Request`.

Success returns `200 OK` with independently nullable values:

```json
{
  "localDate": "2026-07-12",
  "localTime": "10:00",
  "timeZoneId": "America/Vancouver"
}
```

The saved time-zone default takes priority over the fallback. Without an
effective time zone, a weekday cannot produce `localDate`. Independent time and
time-zone defaults can still be returned.

With complete defaults, the API returns the first matching weekly local start
whose UTC instant is strictly later than backend time and is not used by any
stored calendar event. Every stored event occupies its instant regardless of
publication status. Past, equal-to-now, occupied, invalid, and ambiguous local
candidates advance by seven local calendar days. Each candidate is converted
independently, preserving the intended wall-clock time across offset changes.

The response is advisory initial form state. It does not reserve the instant,
and `POST /api/calendar-events` keeps the authoritative duplicate check. It
does not modify existing events, create requests, or provider state.

## Get Calendar Event

```text
GET /api/calendar-events/{calendarEventId}
```

Returns a single calendar event by id with its per-platform publication state.
Requires the `CalendarEvents.Read` scope. The scheduled-start and text fields
match their list counterparts, carrying the wall-clock local start and time
zone so the UI edit form can repopulate from stored local time. It also carries
`scheduledStartUtc`, the same instant as a UTC ISO-8601 string. The details
response does not include the list-only aggregate `publicationStatus`. The edit
form shows this translation and updates it from editable start controls when
`canUpdate` is true.

Unlike a list item, the details response also carries `platforms`: one entry
per active registered platform with its publish status, plus orphan history
rows for platforms deleted after publishing this event, so a client can render
the event details and its publish state from one read. This is the only
endpoint that exposes authoritative per-platform state; there is no separate
event-platform listing route. The list endpoint exposes only the informational
aggregate and never carries `platforms`. Details compose per-platform state
from authoritative `PlatformPublications` rows and do not use the list index.

Success response (`200 OK`):

```json
{
  "calendarEventId": "6f9619ff8b864fb5bdfd4f5c2f2f16a1",
  "start": {
    "localDateTime": "2026-06-06T10:00:00",
    "timeZoneId": "America/Vancouver"
  },
  "scheduledStartUtc": "2026-06-06T17:00:00+00:00",
  "displayTitle": "Saturday stream",
  "canUpdate": true,
  "canDelete": true,
  "thumbnail": {
    "fileName": "stream.png",
    "contentType": "image/png",
    "sizeBytes": 123456,
    "width": 1280,
    "height": 720,
    "updatedUtc": "2026-06-01T12:00:00+00:00"
  },
  "canUpdateThumbnail": true,
  "texts": [
    {
      "fieldKey": "text1",
      "label": "Title",
      "type": "ShortText",
      "maxLength": 50,
      "value": "Saturday stream"
    },
    {
      "fieldKey": "text2",
      "label": "Description",
      "type": "LongText",
      "maxLength": 2500,
      "value": "Description for Saturday stream"
    }
  ],
  "platforms": [
    {
      "platformId": "4fb4a32f3f344de1a7c3a9f4a2f94918",
      "platformName": "Main YouTube channel",
      "platformType": "YouTube",
      "status": "NotPublished",
      "externalResourceId": null,
      "thumbnailStatus": "NotConfigured",
      "publishedUtc": null,
      "platformDeletedUtc": null,
      "canPublish": true,
      "canDeletePublication": false,
      "canPreviewPublishingContent": true
    }
  ]
}
```

Current behavior:

- Unknown `calendarEventId` returns `404 Not Found`.
- `canUpdate` is `true` only when the event has no platform publication rows.
  Any current platform publication row, including `Publishing`, `Published`,
  and orphan history rows, makes event update state-ineligible.
- `canDelete` is `true` only when the event has no platform publication rows.
  It is separate from platform-row `canDeletePublication`.
- `thumbnail` is the current thumbnail metadata or `null`. It never includes
  the internal blob name or image bytes.
- `canUpdateThumbnail` is `true` only when the event has no platform
  publication rows. It uses the same row-based lock as `canUpdate` and
  `canDelete`.
- Deleting platform publication rows through the platform-publication delete
  route unlocks event update and event delete when no publication rows remain.
- `platforms` is `[]` when no platforms are registered.
- `status` is `NotPublished`, `Publishing`, or `Published`. An active platform
  with no stored publication row is reported as a computed `NotPublished` item;
  no row is created just to read state.
- `externalResourceId` and `publishedUtc` are populated for `Published` items
  and are `null` otherwise.
- `thumbnailStatus` is `NotConfigured`, `Applied`, or `Failed` for YouTube
  rows. It is `null` for providers that do not support thumbnail application,
  including WordPress.
- `platformDeletedUtc` is set only on orphan history items whose platform was
  deleted. Orphan items carry `canPublish: false` and
  `canDeletePublication: false`.
- `canPublish` is `true` only for an active platform whose publication is
  `NotPublished` and whose calendar event start is future by backend UTC time.
- `canDeletePublication` is `true` only for an active platform whose
  publication is `Published`, has an `externalResourceId`, and whose calendar
  event start is future by backend UTC time.
- `canPreviewPublishingContent` is `true` for an active `NotPublished` platform
  row, for in-progress or published active rows with a stored content snapshot,
  and for orphan `Published` rows with a stored content snapshot. The details
  response does not embed rendered title or description content.
- The `CalendarEventDetails` edit route
  (`/calendar-events/{calendarEventId}/edit`) consumes this endpoint to load an
  event into the form. Edit mode uses the returned `texts` snapshot and does
  not reshape the event from the current settings row.

## Update Calendar Event

```text
PUT /api/calendar-events/{calendarEventId}
```

Replaces the scheduled start and text values of an existing calendar event in
place. Requires the `CalendarEvents.Write` scope.

Update converts and validates the submitted scheduled start before persistence,
then validates submitted text values against the event's stored text snapshot,
not against the current event text fields setting.

Request body:

```json
{
  "start": {
    "localDateTime": "2026-06-07T10:30:00",
    "timeZoneId": "America/Vancouver"
  },
  "texts": [
    {
      "fieldKey": "text1",
      "value": "Saturday stream edited"
    },
    {
      "fieldKey": "text2",
      "value": "Updated description for Saturday stream"
    }
  ]
}
```

Success response (`200 OK`):

```json
{
  "calendarEventId": "6f9619ff8b864fb5bdfd4f5c2f2f16a1"
}
```

The update is an in-place write of the scheduled start fields and event text
snapshot values. The event identity and stored field definitions are left
unchanged. The list page re-fetches its current page after a successful edit,
so the new scheduled start and text values appear in the active sort order.

Current behavior and error mapping:

- Unknown `calendarEventId` returns `404 Not Found`.
- Missing `start` returns `400 Bad Request`.
- Invalid, unknown, skipped, or repeated local scheduled-start values return
  `400 Bad Request`.
- Invalid JSON returns `400 Bad Request` with a plain string message.
- Missing request body returns `400 Bad Request` with a plain string message.
- Missing, unknown, duplicate, blank, or over-length text values return
  `400 Bad Request`.
- Any platform publication row for the event returns `409 Conflict`. Use the
  platform-publication delete route to clean up completed provider
  publications before updating the event.
- Detected duplicate scheduled starts return `409 Conflict`. Duplicate
  detection is best-effort for normal sequential writes; concurrent duplicate
  writes are an accepted risk.
- The `CalendarEventDetails` edit route
  (`/calendar-events/{calendarEventId}/edit`) consumes this endpoint on save,
  sending the event's scheduled start and text values.

Current limitations:

- Broader command validation does not return stable structured error bodies.
- A concurrent edit that loses the ETag race surfaces as `500`.

## Delete Calendar Event

```text
DELETE /api/calendar-events/{calendarEventId}
```

Deletes a calendar event that has no platform publication rows. Requires the
`CalendarEvents.Write` scope. Deleting a calendar event is local
application-data cleanup only; this endpoint does not contact YouTube,
WordPress, or any other provider. If the event has thumbnail metadata, the
stored thumbnail blob is deleted best-effort after the event row is deleted.

Success returns `204 No Content` with an empty body.

Rejected states and error mapping:

- Unknown non-empty `calendarEventId` returns `404 Not Found`. Calendar event
  IDs are opaque; there is no public id-format validation contract.
- Any platform publication row for the event returns `409 Conflict`. This is
  the same row-based lock used by event update. Use the
  platform-publication delete route to clean up completed provider
  publications before deleting the event.
- A row that disappears between the existence check and delete write returns
  `204 No Content` because the requested end state already holds.
- A thumbnail blob cleanup failure does not change the delete result because
  the event row has already been deleted.

Scope and proof-of-concept limitations:

- The delete is a hard delete: removed events are not recoverable. Tombstones,
  recycle-bin behavior, audit retention, and restore are out of scope.
- The `CalendarEventDetails` edit route
  (`/calendar-events/{calendarEventId}/edit`) consumes this endpoint from its
  Delete action.

## Related Contracts

- Event text fields: [`event-text-fields.md`](event-text-fields.md)
- Calendar event start defaults:
  [`calendar-event-start-defaults.md`](calendar-event-start-defaults.md)
- Calendar-event thumbnails:
  [`calendar-event-thumbnails.md`](calendar-event-thumbnails.md)
- Platform publications:
  [`platform-publications.md`](platform-publications.md)
