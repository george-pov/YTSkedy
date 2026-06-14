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

## List Calendar Events By Month

```text
GET /api/calendar-events?year={year}&month={month}
```

Query parameters:

- `year`: required integer from `1000` through `9999`.
- `month`: required integer from `1` through `12`.

Month and year refer to the event's submitted local calendar month. The API
does not infer the current month, current year, or machine local time zone.

Success response:

```json
[
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
]
```

`status` is `Draft`, `Publishing`, or `Published`. New events are `Draft`. A
`Publishing` row has a publish in progress. Rows stored before the status field
existed are read as `Draft`.

No matching rows return `200 OK` with `[]`.

Current invalid query behavior:

- Missing `year` or `month` returns `400 Bad Request`.
- Empty values return `400 Bad Request`.
- Non-integer values return `400 Bad Request`.
- `year` outside `1000` through `9999` returns `400 Bad Request`.
- `month` outside `1` through `12` returns `400 Bad Request`.
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
