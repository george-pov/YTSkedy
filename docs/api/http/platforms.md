# Platform Publishing HTTP Contract

Platform and platform-publishing endpoints are hosted by `YTSkedy.AzureFunctions`
under the Azure Functions `/api` prefix.

A `platform` is a configured publishing destination such as a YouTube channel. A
calendar event is a provider-neutral scheduling record and carries no publish
status of its own. Publish state lives in a `platform publication`: the
relationship between one calendar event and one platform. Publishing always
targets an explicit platform id; there is no calendar-event-level publish route.

The first implemented provider is YouTube. `WordPress` is recognized as a
platform type value but cannot be configured yet because no WordPress publish
settings are defined; create and update reject a non-YouTube type with
`400 Bad Request`.

## Authorization

Platform HTTP triggers run at `AuthorizationLevel.Anonymous`. The security
boundary is the worker-side bearer-token middleware, not the Functions host key
check. These endpoints reuse the calendar-event scopes; no new scope was added.

Every call must:

- Present a Microsoft Entra External ID access token via
  `Authorization: Bearer <token>`. Missing, invalid, expired, wrong-audience,
  or wrong-issuer tokens return `401`.
- Carry the scope required by the endpoint (`CalendarEvents.Read` for `GET`,
  `CalendarEvents.Write` for `POST`, `PUT`, `DELETE`, and `publish`). Wrong
  scope returns `403`.
- Carry the `CalendarEvents.Operator` app role in the `roles` claim. Missing
  role returns `403`.

`x-functions-key` is not accepted on these endpoints. Local manual checks
acquire a bearer token via the `az`-based recipe documented in
`docs/api/development/build-and-test.md`.

## Platform Shape

A platform is returned as:

```json
{
  "platformId": "4fb4a32f3f344de1a7c3a9f4a2f94918",
  "name": "Main YouTube channel",
  "type": "YouTube",
  "publishSettings": {
    "credentials": "main-youtube-channel",
    "privacyStatus": "private",
    "selfDeclaredMadeForKids": false
  }
}
```

- `platformId` is a server-generated 32-character lowercase hex GUID.
- `name` is required, trimmed, at most 50 characters, and globally unique across
  all platforms.
- `type` is `YouTube` (the only configurable type in this slice). It is set on
  create and is immutable because it determines the publish-settings schema and
  the provider adapter.
- `publishSettings` is the only settings object on a platform. For YouTube it
  carries `credentials` (a non-secret reference name for externally configured
  credential material), `privacyStatus` (`private`, `public`, or `unlisted`),
  and `selfDeclaredMadeForKids`. No secret material is ever stored or returned.

## List Platforms

```text
GET /api/platforms
GET /api/platforms?type=YouTube
```

Returns every configured platform. The optional `type` query parameter filters
by platform type (case-insensitive: `YouTube` or `WordPress`).

Success response (`200 OK`):

```json
{
  "items": [
    {
      "platformId": "4fb4a32f3f344de1a7c3a9f4a2f94918",
      "name": "Main YouTube channel",
      "type": "YouTube",
      "publishSettings": {
        "credentials": "main-youtube-channel",
        "privacyStatus": "private",
        "selfDeclaredMadeForKids": false
      }
    }
  ]
}
```

Status codes:

- `200 OK` with `items: []` when no platforms exist.
- `400 Bad Request` when `type` is not a recognized platform type, or when the
  parameter is repeated.

## Get Platform

```text
GET /api/platforms/{platformId}
```

Returns one platform by id using the single platform shape.

Status codes:

- `200 OK` when found.
- `404 Not Found` when no platform has the id.

## Create Platform

```text
POST /api/platforms
```

Request body:

```json
{
  "name": "Main YouTube channel",
  "type": "YouTube",
  "publishSettings": {
    "credentials": "main-youtube-channel",
    "privacyStatus": "private",
    "selfDeclaredMadeForKids": false
  }
}
```

`selfDeclaredMadeForKids` defaults to `false` when omitted. Success returns
`200 OK` with the created platform (including its generated `platformId`).

Status codes:

- `200 OK` with the created platform.
- `400 Bad Request` when the body is missing or not valid JSON.
- `400 Bad Request` when `name` is empty or longer than 50 characters.
- `400 Bad Request` when `type` is not a recognized platform type.
- `400 Bad Request` when `type` is recognized but not supported for create
  (anything other than `YouTube` in this slice).
- `400 Bad Request` when `publishSettings` is missing, `credentials` is empty,
  or `privacyStatus` is not `private`, `public`, or `unlisted`.
- `409 Conflict` when another platform already uses the same name.

## Update Platform

```text
PUT /api/platforms/{platformId}
```

Replaces the name and publish settings of an existing platform. `type` is
immutable and is not accepted in the update body.

Request body:

```json
{
  "name": "Main YouTube channel",
  "publishSettings": {
    "credentials": "main-youtube-channel",
    "privacyStatus": "unlisted",
    "selfDeclaredMadeForKids": false
  }
}
```

Success returns `200 OK` with the updated platform.

Status codes:

- `200 OK` with the updated platform.
- `400 Bad Request` for a missing or invalid body, invalid name, or invalid
  publish settings (same rules as create).
- `404 Not Found` when no platform has the id.
- `409 Conflict` when renaming to a name already used by another platform.
- `409 Conflict` when the platform has a publication that is currently
  `Publishing`.

## Delete Platform

```text
DELETE /api/platforms/{platformId}
```

Deletes a configured platform. Deletion preserves `Published` publication rows
as read-only orphan history by stamping `platformDeletedUtc` on them before the
platform row is removed; it does not contact any provider and does not remove
external resources already created on the platform.

Status codes:

- `204 No Content` when the platform is deleted.
- `404 Not Found` when no platform has the id.
- `409 Conflict` when the platform has any publication that is currently
  `Publishing`.

After delete, the platform no longer appears in `GET /api/platforms`, but its
`Published` publications remain visible as orphan history in the calendar event
detail response with `platformDeletedUtc` set and `canPublish: false`.

## Event Platform Publication State

The per-platform publication state of a calendar event is returned by the
calendar event detail endpoint `GET /api/calendar-events/{calendarEventId}` as
its `platforms` array (see [`calendar-events.md`](calendar-events.md), which
documents the item fields and the `status` / `canPublish` / orphan-history
semantics). There is no separate event-platform listing endpoint.

## Publish Calendar Event To Platform

```text
POST /api/calendar-events/{calendarEventId}/platforms/{platformId}/publish
```

Publishes one calendar event to one selected platform. Both ids come from the
route. The request body is empty in this iteration:

```json
{}
```

For a YouTube platform this creates a scheduled YouTube `liveBroadcast` using the
event's English title and optional description, the stored UTC scheduled start,
and the platform's `privacyStatus` and `selfDeclaredMadeForKids`. The created
broadcast id is returned as the provider-neutral `externalResourceId`.

Success response (`200 OK`):

```json
{
  "calendarEventId": "20260606T170000Z",
  "platformId": "4fb4a32f3f344de1a7c3a9f4a2f94918",
  "platformName": "Main YouTube channel",
  "platformType": "YouTube",
  "status": "Published",
  "externalResourceId": "abc123youtubeid",
  "publishedUtc": "2026-06-22T12:00:00+00:00"
}
```

Status codes:

- `200 OK` when the publish succeeds and the publication row is marked
  `Published`.
- `400 Bad Request` when the event start is not in the future.
- `400 Bad Request` when provider-required content is missing. For YouTube the
  event must have an English (`en`) description with a non-empty title.
- `404 Not Found` when the calendar event id or the platform id does not exist.
- `409 Conflict` when the publication is already `Published`.
- `409 Conflict` when a publish is already in progress (`Publishing`), including
  when a concurrent request wins the reservation race.
- `409 Conflict` when the publication is orphaned because the platform was
  deleted; orphan history is read-only.
- `501 Not Implemented` when no provider adapter serves the platform type.
- `502 Bad Gateway` when the provider call fails.
- `500 Internal Server Error` when the external resource was created but the
  publication row could not be finalized. There is no provider cleanup in this
  slice, so the orphaned external resource id is logged for operator follow-up.

State conflicts are evaluated before content validation, so an already-published
or in-progress publication returns `409` even when the start is in the past.

## Manual Checks

Manual `.http` checks live under:

```text
src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/Platforms/
src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/CalendarEvents/
```

Platform CRUD and platform delete checks are in the `Platforms/` folder. Event
platform listing and platform-aware publish checks are in the `CalendarEvents/`
folder because they hang off the calendar-event route.

Before sending local requests:

- Reset the application-owned tables for the feature environment when starting
  from a clean state. See the reset note in
  `src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/Platforms/ResetFeatureData.http`.
- Start Azurite or provide an Azure Storage connection string.
- Configure at least one YouTube channel credential set for publish checks. See
  [`../operations/youtube-publish-setup.md`](../operations/youtube-publish-setup.md).
- Start the Azure Functions host.
- Select the `local` environment in the `.http` editor and set tokens in
  `http-client.env.json.user`.

Keep deployed URLs, bearer access tokens, and personal values in
`http-client.env.json.user`, not in tracked environment files.
