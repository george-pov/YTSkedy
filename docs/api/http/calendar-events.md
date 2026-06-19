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
  `CalendarEvents.Write` for `POST`, `PUT`, and `DELETE`). Wrong scope returns
  `403`. The publish endpoint
  (`POST /api/calendar-events/{calendarEventId}/publish`) requires
  `CalendarEvents.Write`.
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
  "calendarEventId": "20260606T170000Z"
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
- `sort`: sort field, one of `scheduledStart`, `status`, `timeZone`, or `title`
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
      "calendarEventId": "20260606T170000Z",
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
      "status": "Draft",
      "canPublish": true,
      "canUpdate": true,
      "canDelete": true
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
- `status` is `Draft`, `Publishing`, or `Published`. New events are `Draft`. A
  `Publishing` row has a publish in progress. Rows stored before the status
  field existed are read as `Draft`.
- `canPublish`, `canUpdate`, and `canDelete` are server-computed action flags
  the UI uses for Publish, edit/Save, and Delete enablement; the client never
  re-derives them from `status`, `scheduledStartUtc`, or descriptions.
  `canPublish` is `true` only for a `Draft` whose start is in the future and
  that has an English (`en`) description. `canUpdate` is `true` only for a
  `Draft`. `canDelete` is `true` for any `Draft` (future or past) and for a
  future `Published` event that has a recorded `youTubeBroadcastId`. The flags
  are advisory: each write re-checks eligibility server-side, so a stale `true`
  can still be rejected.
- A page past the end returns `200 OK` with `items` as `[]` and the real
  `totalCount`. No stored events returns `items` as `[]` with `totalCount` `0`.

Current invalid query behavior:

- `page` that is not a non-negative integer returns `400 Bad Request`.
- `pageSize` outside `1` through `100`, or not an integer, returns
  `400 Bad Request`.
- `sort` outside `scheduledStart`, `status`, `timeZone`, `title` returns
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

Returns a single calendar event by id. Requires the `CalendarEvents.Read` scope.
The response item has the same shape as one `items[]` entry from the list
endpoint, carrying the wall-clock local start and time zone (not the UTC
instant) so the UI edit form can repopulate from stored local time. It also
carries `scheduledStartUtc`, the same instant as a UTC ISO-8601 string, which the
edit form shows as a read-only translation of the local start.

Success response (`200 OK`):

```json
{
  "calendarEventId": "20260606T170000Z",
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
  "status": "Draft",
  "canPublish": true,
  "canUpdate": true,
  "canDelete": true
}
```

Current behavior:

- Unknown `calendarEventId` returns `404 Not Found`.
- The response carries the same `canPublish`, `canUpdate`, and `canDelete`
  action flags as a list item, computed for this event.
- The `CalendarEventDetails` edit route (`/calendar-events/{calendarEventId}/edit`)
  consumes this endpoint to load an event into the form.

## Update Calendar Event

```text
PUT /api/calendar-events/{calendarEventId}
```

Replaces the localized descriptions of an existing `Draft` calendar event in
place. Requires the `CalendarEvents.Write` scope. Only `Draft` events are
updatable, and only the descriptions can change: the scheduled start is
immutable because the event id is derived from its UTC start instant, so the
request body carries no start. Once an event is `Publishing` or `Published` its
descriptions are frozen so they cannot drift from the metadata already sent to
YouTube.

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
  "calendarEventId": "20260606T170000Z"
}
```

The update is an in-place ETag-conditional write of the descriptions blob; the
event identity, scheduled start, and status are left unchanged. The list page
re-fetches its current page after a successful edit, so the new descriptions
appear in the active sort order.

Current behavior and error mapping:

- Unknown `calendarEventId` returns `404 Not Found`.
- A `Publishing` or `Published` event returns `409 Conflict` with wording such
  as `Calendar event '{calendarEventId}' cannot be updated in its current
  state.` and is not modified, so already-published descriptions cannot drift
  from the metadata sent to YouTube.
- Invalid JSON returns `400 Bad Request` with a plain string message.
- Missing request body returns `400 Bad Request` with a plain string message.
- The `CalendarEventDetails` edit route (`/calendar-events/{calendarEventId}/edit`)
  consumes this endpoint on save, sending the English and Russian descriptions.

Production release requirements:

- Broader command validation must return stable client-facing errors.
- A concurrent edit that loses the ETag race currently surfaces as `500`; it
  must map to a stable conflict response.

## Publish Calendar Event

```text
POST /api/calendar-events/{calendarEventId}/publish
```

Creates a scheduled YouTube live broadcast for a `Draft` calendar event and
flips the stored event status to `Published`. Requires the
`CalendarEvents.Write` scope. The request body is ignored (send `{}`).

The broadcast is created with the static defaults in
[`../configuration.md`](../configuration.md) (`YouTubeBroadcast:*`). Title,
description, and scheduled start come from the event: the English (`en`)
localized title and description, and the stored UTC start instant.

Success response:

```json
{
  "calendarEventId": "20260606T170000Z",
  "status": "Published",
  "youTubeBroadcastId": "abc123youtubeid"
}
```

Current behavior and error mapping:

- Unknown `calendarEventId` returns `404 Not Found`.
- Before calling YouTube the event is reserved by moving it atomically from
  `Draft` to `Publishing`. A second concurrent publish loses that reservation
  and is rejected with `409 Conflict`, so no duplicate broadcast is created.
- An event already in `Published` or `Publishing` status returns `409 Conflict`
  and does not call YouTube.
- A start instant that is not in the future returns `400 Bad Request` and does
  not call YouTube.
- An event with no English (`en`) description returns `400 Bad Request`.
- Any failure from the YouTube call releases the reservation back to `Draft`,
  surfaces as `500`, and leaves the event retryable. A hard interruption (such
  as a host crash) between the reservation and the result can leave the event
  stuck in `Publishing`; recovering it currently requires manual inspection.
  There is no automatic retry, partial-state reconciliation, or detailed error
  surface in this proof-of-concept iteration.

Proof-of-concept limitations:

- Publishing creates a scheduled `liveBroadcast` only. It does not create or
  bind a `liveStream`, upload a thumbnail, or transition broadcast state.
- Credentials are shared and static, so every publish targets the single
  channel that minted the refresh token. A per-user Google OAuth flow is
  deferred. See [`../configuration.md`](../configuration.md).

## Delete Calendar Event

```text
DELETE /api/calendar-events/{calendarEventId}
```

Deletes a calendar event. Requires the `CalendarEvents.Write` scope. A `Draft`
event is local cleanup that never contacts YouTube; a future `Published` event
also removes its scheduled YouTube `liveBroadcast` before the local row. Use the
`canDelete` response flag to decide whether to offer the action; the backend
re-checks eligibility, so a stale `canDelete` is still enforced here.

Success returns `204 No Content` with an empty body.

Deletable states:

- A `Draft` event (future or past) is hard-deleted locally and the endpoint
  returns `204 No Content`. Deleting a draft is local cleanup, not a YouTube
  operation, so it never contacts YouTube; the scheduled start does not affect
  Draft delete eligibility.
- A future `Published` event with a recorded `youTubeBroadcastId` deletes the
  YouTube `liveBroadcast` first and then removes the local row, returning
  `204 No Content`. The broadcast is deleted before the local row so the
  external resource cannot be orphaned.

YouTube cleanup policy for a future `Published` delete:

- A YouTube not-found for the stored broadcast id is success-equivalent: the
  intended end state (no broadcast) already holds, so local cleanup continues
  and the endpoint returns `204 No Content`.
- Any other YouTube failure returns `502 Bad Gateway` with a generic body
  (`The YouTube broadcast could not be deleted.`) and keeps the local row so the
  operator can retry. Provider error details are never surfaced.
- If the local row has already disappeared after a successful YouTube delete,
  the endpoint still returns `204 No Content` because both the external and
  local resources are gone.

Rejected states and error mapping:

- Unknown `calendarEventId` returns `404 Not Found`. A syntactically invalid
  non-empty id also returns `404 Not Found`, matching the by-id read; there is
  no `400` id-format contract.
- A `Publishing` event and a past `Published` event return `409 Conflict` and
  are not deleted.
- A future `Published` event with a missing or blank `youTubeBroadcastId`
  returns `409 Conflict` with diagnostic wording such as `Calendar event
  '{calendarEventId}' cannot be deleted because no YouTube broadcast id is
  recorded.` and keeps the local row.
- Other not-deletable states use `409 Conflict` with wording that is not
  Draft-only, such as `Calendar event '{calendarEventId}' cannot be deleted in
  its current state.`
- The `Draft` delete is an ETag-conditional write on the loaded `Draft` row. If
  a concurrent write moves the event out of `Draft` first, the endpoint returns
  `409 Conflict`; if the row is already gone, it returns `404 Not Found`. The
  future `Published` path takes one application read, deletes the broadcast, and
  then deletes the local row by id without re-reading status.

Scope and proof-of-concept limitations:

- Deleting a `Published` event removes only the scheduled `liveBroadcast`,
  matching the publish proof of concept. It does not delete or unbind a
  `liveStream`, and there is no confirmation prompt, retry, audit retention, or
  restore in this iteration.
- The delete is a hard delete: removed events are not recoverable. Tombstones,
  recycle-bin behavior, audit retention, and restore are out of scope.
- The `CalendarEventDetails` edit route
  (`/calendar-events/{calendarEventId}/edit`) consumes this endpoint from its
  Delete action, shown only when the loaded event's `canDelete` flag is `true`.

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
