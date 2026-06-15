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
  `CalendarEvents.Write` for `POST`). Wrong scope returns `403`.
  The publish endpoint (`POST /api/calendar-events/{calendarEventId}/publish`)
  requires `CalendarEvents.Write`.
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
      "status": "Draft"
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
- `status` is `Draft`, `Publishing`, or `Published`. New events are `Draft`. A
  `Publishing` row has a publish in progress. Rows stored before the status
  field existed are read as `Draft`.
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
