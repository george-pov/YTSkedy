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

## Event Text Fields Setting

```text
GET /api/settings/event-text-fields
PUT /api/settings/event-text-fields
```

`GET` requires `CalendarEvents.Read`; `PUT` requires `CalendarEvents.Write`.
The setting defines the current text fields used by newly created calendar
events. Existing new-shape calendar events keep the field snapshot stored on
the event and are not reshaped when this setting changes.

When no settings row exists, `GET` returns the backend default list:

```json
{
  "fields": [
    {
      "fieldKey": "text1",
      "label": "Title",
      "type": "ShortText",
      "maxLength": 50
    },
    {
      "fieldKey": "text2",
      "label": "Description",
      "type": "LongText",
      "maxLength": 2500
    }
  ]
}
```

`PUT` accepts the same ordered `fields` array. The backend derives `fieldKey`
values from order as `text1`, `text2`, `text3`, and so on. Clients may send
existing keys, but the response contains the normalized keys.

```json
{
  "fields": [
    {
      "fieldKey": "text1",
      "label": "Title",
      "type": "ShortText",
      "maxLength": 50
    },
    {
      "fieldKey": "text2",
      "label": "Summary",
      "type": "ShortText",
      "maxLength": 100
    },
    {
      "fieldKey": "text3",
      "label": "Description",
      "type": "LongText",
      "maxLength": 2500
    }
  ]
}
```

Current behavior:

- `type` is `ShortText` or `LongText`.
- `label` is required.
- `maxLength` must be a positive whole number.
- At least one field is required.
- Invalid JSON, a missing body, or an invalid field list returns
  `400 Bad Request`.

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
- `sort`: sort field, one of `scheduledStart`, `timeZone`, or `title`
  (case-insensitive). Default `scheduledStart`. `scheduledStart` orders by the
  UTC start instant. `title` orders by `displayTitle`.
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
  offset string. The UI list renders `scheduledStartUtc`; the create/edit form
  works in local time and zone.
- `displayTitle` is the backend-defined representative title for list display
  and `title` sorting. It is the first `ShortText` value in the stored event
  snapshot, falling back to the first text value when the snapshot has no short
  text field.
- `texts` is the event's stored snapshot. It carries enough field metadata for
  clients to display or edit the event without consulting the current setting.
- A page past the end returns `200 OK` with `items` as `[]` and the real
  `totalCount`. No stored events returns `items` as `[]` with `totalCount` `0`.

Current invalid query behavior:

- `page` that is not a non-negative integer returns `400 Bad Request`.
- `pageSize` outside `1` through `100`, or not an integer, returns
  `400 Bad Request`.
- `sort` outside `scheduledStart`, `timeZone`, `title` returns
  `400 Bad Request`.
- `direction` outside `asc`, `desc` returns `400 Bad Request`.
- Supplying only one of `year`/`month` returns `400 Bad Request`; an
  out-of-range `year` or `month` returns `400 Bad Request`.
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
ISO-8601 string. The edit form shows this translation and updates it from
editable start controls when `canUpdate` is true.

Unlike a list item, the details response also carries `platforms`: one entry
per active registered platform with its publish status, plus orphan history
rows for platforms deleted after publishing this event, so a client can render
the event details and its publish state from one read. This is the only
endpoint that exposes per-event publication state; there is no separate
event-platform listing route. The calendar event itself stays
provider-neutral; the publish state is composed at read time and is not stored
on the event. The calendar event list endpoint stays provider-neutral and does
not carry `platforms`.

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

## Upload Calendar Event Thumbnail

```text
PUT /api/calendar-events/{calendarEventId}/thumbnail
```

Uploads or replaces one current thumbnail for a calendar event. Requires the
`CalendarEvents.Write` scope. The request must be `multipart/form-data` with
one file part named `thumbnail`.

The backend accepts JPEG and PNG thumbnails only. Validation checks the file
extension (`.jpg`, `.jpeg`, or `.png`), content type (`image/jpeg` or
`image/png`), byte size (2 MB or smaller), and readable image dimensions.
The backend does not crop, resize, rotate, recompress, optimize, strip
metadata, enforce a 16:9 aspect ratio, or enforce a minimum width.

Success response (`200 OK`):

```json
{
  "fileName": "stream.png",
  "contentType": "image/png",
  "sizeBytes": 123456,
  "width": 1280,
  "height": 720,
  "updatedUtc": "2026-06-01T12:00:00+00:00"
}
```

Status codes:

- `200 OK` when the thumbnail is stored and metadata is saved.
- `400 Bad Request` when the upload is not `multipart/form-data`, the
  `thumbnail` part is missing, the form body cannot be read, or validation
  fails.
- `404 Not Found` when the calendar event id does not exist.
- `409 Conflict` when any platform publication row exists for the event.

## Get Calendar Event Thumbnail

```text
GET /api/calendar-events/{calendarEventId}/thumbnail
```

Returns the current thumbnail bytes. Requires the `CalendarEvents.Read` scope.
The response body is the image content and the response content type is the
stored thumbnail content type. Browser clients should fetch this route through
the typed API client and create an object URL rather than using the protected
route directly as an image URL.

Status codes:

- `200 OK` with image bytes when metadata and blob content exist.
- `404 Not Found` when the calendar event id does not exist.
- `404 Not Found` when the event has no thumbnail metadata or the stored blob
  content is missing.

## Delete Calendar Event Thumbnail

```text
DELETE /api/calendar-events/{calendarEventId}/thumbnail
```

Deletes the current thumbnail metadata and blob content. Requires the
`CalendarEvents.Write` scope.

Status codes:

- `204 No Content` when the thumbnail is deleted.
- `404 Not Found` when the calendar event id does not exist.
- `404 Not Found` when the event has no thumbnail metadata.
- `409 Conflict` when any platform publication row exists for the event.

## Get Platform Publishing Content

```text
GET /api/calendar-events/{calendarEventId}/platforms/{platformId}/publishing-content
```

Returns row-level publishing content for one calendar event and one platform.
Requires the `CalendarEvents.Read` scope. Use the `canPreviewPublishingContent`
flag from the calendar event details row to decide whether to show the action.

For active `NotPublished` rows, the endpoint recalculates current preview
content from the calendar event's stored text snapshot, the platform's
`publishingContent`, the selected templates, fixed date tokens, and available
platform reference-key tokens. A placeholder whose name exactly matches an
active platform's `referenceKey` is replaced with that platform publication's
`externalResourceId` when the same calendar event already has a `Published`
row for that platform. Preview content is not persisted. For `Publishing`,
`Published`, and orphan `Published` rows with a content snapshot, the endpoint
returns the stored snapshot.

Success response for a recalculated preview (`200 OK`):

```json
{
  "kind": "Preview",
  "title": "Live on July 4, 2030: Saturday stream",
  "description": "Description for Saturday stream"
}
```

Success response for a stored snapshot (`200 OK`):

```json
{
  "kind": "Snapshot",
  "title": "Published title",
  "description": null
}
```

Status codes:

- `200 OK` with `kind: "Preview"` for active unpublished rows that can render
  current content.
- `200 OK` with `kind: "Snapshot"` for publishing, published, or orphan
  published rows with stored content snapshots.
- `404 Not Found` when the calendar event id does not exist.
- `404 Not Found` when an active platform is required and the platform id does
  not exist.
- `409 Conflict` when preview is unavailable for the row state, a selected
  template is missing, or the rendered title is empty.

Preview leaves unresolved well-formed placeholders visible in the returned
content. Publish rejects unresolved well-formed placeholders before any
provider call, as documented in [`platforms.md`](platforms.md). A reference-key
placeholder stays unresolved until the matching platform has a published
external resource id for that same calendar event.

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

## Platform Publishing

A calendar event is a provider-neutral scheduling record and carries no publish
status of its own. There is no calendar-event-level publish route. Publishing
state lives in platform publications, and publishing always targets an
explicit platform id.

The publication state of an event is part of the calendar event details
response (`GET /api/calendar-events/{calendarEventId}`; see the `platforms`
array in [Get Calendar Event](#get-calendar-event)). The publish action is
documented in [`platforms.md`](platforms.md):

- `POST /api/calendar-events/{calendarEventId}/platforms/{platformId}/publish`
  publishes the event to one selected platform. The response is row-level only;
  clients that need root `canUpdate` or `canDelete` must refresh the calendar
  event details response.
- `GET /api/calendar-events/{calendarEventId}/platforms/{platformId}/publishing-content`
  returns a recalculated preview for active unpublished rows or a stored
  snapshot for rows where publishing has started.
- `DELETE /api/calendar-events/{calendarEventId}/platforms/{platformId}/publication`
  deletes one completed provider publication and resets the platform row when
  cleanup succeeds. The response is row-level only; clients that need root
  `canUpdate` or `canDelete` must refresh the calendar event details response.
- `PUT /api/calendar-events/{calendarEventId}/thumbnail`,
  `GET /api/calendar-events/{calendarEventId}/thumbnail`, and
  `DELETE /api/calendar-events/{calendarEventId}/thumbnail` manage the optional
  event thumbnail while no platform publication rows exist.

## Manual Checks

Manual `.http` checks live under:

```text
src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/CalendarEvents/
```

Thumbnail upload, read, and delete checks are in `EventThumbnails.http`.

Before sending local requests:

- Start Azurite or provide an Azure Storage connection string.
- Start the Azure Functions host.
- Select the `local` environment in the `.http` editor.
- Use the host port from the Azure Functions launch profile. The current local
  default is `http://localhost:7087`.

Keep deployed URLs, bearer access tokens, and personal values in
`http-client.env.json.user`, not in tracked environment files. Function keys
no longer apply to these endpoints.
