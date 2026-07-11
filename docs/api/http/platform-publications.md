# Platform Publications HTTP Contract

Canonical HTTP contract for platform publication state, publishing-content
preview, publish, and publication deletion.

## Authorization

Platform HTTP triggers run at `AuthorizationLevel.Anonymous`. The security
boundary is the worker-side bearer-token middleware, not the Functions host key
check. These endpoints reuse the calendar-event scopes; no new scope was added.

Every call must:

- Present a Microsoft Entra External ID access token via
  `Authorization: Bearer <token>`. Missing, invalid, expired, wrong-audience,
  or wrong-issuer tokens return `401`.
- Carry the scope required by the endpoint (`CalendarEvents.Read` for `GET`,
  `CalendarEvents.Write` for `POST`, `PUT`, `DELETE`, publish, and
  publication delete). Wrong scope returns `403`.
- Carry the `CalendarEvents.Operator` app role in the `roles` claim. Missing
  role returns `403`.

Function keys are not part of authorization for these endpoints.

## Event Platform Publication State

The per-platform publication state of a calendar event is returned by the
calendar event details endpoint `GET /api/calendar-events/{calendarEventId}` as
its `platforms` array (see [`calendar-events.md`](calendar-events.md), which
documents the item fields and the `status` / `canPublish` / orphan-history
semantics). Each row also carries `canDeletePublication`, the backend-computed
flag that tells clients whether the platform publication can be deleted, and
`canPreviewPublishingContent`, the backend-computed flag that tells clients
whether row-level publishing content can be read. There is no separate
event-platform listing endpoint.

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
provider call, as documented by the publish contract below. A reference-key
placeholder stays unresolved until the matching platform has a published
external resource id for that same calendar event.

## Publish Calendar Event To Platform

```text
POST /api/calendar-events/{calendarEventId}/platforms/{platformId}/publish
```

Publishes one calendar event to one selected platform. Both ids come from the
route. The request body is empty:

```json
{}
```

Before provider calls, the backend renders title and description from the
platform's selected title and description templates using token values from the
calendar event's stored text snapshot, fixed date tokens, and available
platform reference-key tokens. A placeholder whose name exactly matches an
active platform's `referenceKey` is replaced with that platform publication's
`externalResourceId` when the same calendar event already has a `Published`
row for that platform. If no matching active platform or published external id
exists, the placeholder remains unchanged. The rendered title and description
are stored as a content snapshot when the publication row enters `Publishing`.
There is no fallback that renders directly from event text fields.

For a YouTube platform this creates a scheduled YouTube `liveBroadcast` using
the rendered title and optional rendered description, the stored UTC scheduled
start, and the platform's `privacyStatus` and `selfDeclaredMadeForKids`. The
created broadcast id is returned as the provider-neutral `externalResourceId`.
When the calendar event has a stored thumbnail, the backend records the created
broadcast id on the local publication row first, then applies the thumbnail to
that YouTube broadcast. Thumbnail application is a separate YouTube API call
from broadcast creation.

For a WordPress platform this discovers the WordPress REST API root from the
configured site URL, then creates a post through logical route
`POST /wp/v2/posts` using Basic Auth with the configured WordPress username and
Application Password. The request maps the rendered title to `title`, the
optional rendered description to `content`, and the platform's `postStatus` to
`status`. It maps `sticky` to the WordPress REST `sticky` field. When
`postStatus` is `future`, it computes `date_gmt` by subtracting
`scheduleOffsetHours` from the calendar event's `scheduledStartUtc`; the
offset must be from `1` through `168`. Other statuses omit `date_gmt`. The
platform's non-empty `categoryIds` array maps to the WordPress `categories`
property in submitted order. When the array is empty, the backend omits
`categories`, allowing WordPress to apply its normal default-category behavior.
WordPress can accept a stale positive ID while silently dropping that category,
so a successful create status alone does not prove category assignment. The
numeric WordPress post id is returned as the provider-neutral
`externalResourceId`. A local `Published` row means the provider resource was
created; YTSkedy does not track the later WordPress transition from `future` to
`publish`.

YouTube success response (`200 OK`):

```json
{
  "platformId": "4fb4a32f3f344de1a7c3a9f4a2f94918",
  "platformName": "Main YouTube channel",
  "platformType": "YouTube",
  "status": "Published",
  "externalResourceId": "abc123youtubeid",
  "thumbnailStatus": "Applied",
  "publishedUtc": "2026-06-22T12:00:00+00:00",
  "platformDeletedUtc": null,
  "canPublish": false,
  "canDeletePublication": true,
  "canPreviewPublishingContent": true
}
```

WordPress success response (`200 OK`):

```json
{
  "platformId": "5aa4a32f3f344de1a7c3a9f4a2f94918",
  "platformName": "Company blog",
  "platformType": "WordPress",
  "status": "Published",
  "externalResourceId": "123",
  "thumbnailStatus": null,
  "publishedUtc": "2026-06-22T12:00:00+00:00",
  "platformDeletedUtc": null,
  "canPublish": false,
  "canDeletePublication": true,
  "canPreviewPublishingContent": true
}
```

Status codes:

- `200 OK` when the publish succeeds and the publication row is marked
  `Published`. The response is the computed event-platform row.
- `200 OK` when YouTube broadcast creation succeeds but thumbnail application
  fails. The row remains `Published`, `externalResourceId` is the created
  broadcast id, and `thumbnailStatus` is `Failed`.
- `400 Bad Request` when the event start is not in the future.
- `404 Not Found` when the calendar event id or the platform id does not exist.
- `409 Conflict` when rendered publishing content is invalid. Invalid content
  includes a missing selected template, an empty rendered title, or an
  unresolved well-formed placeholder such as `{{ missingToken }}` or a
  reference-key placeholder whose published external resource id is not
  available yet.
- `409 Conflict` when the publication is already `Published`.
- `409 Conflict` when a publish is already in progress (`Publishing`),
  including when a concurrent request wins the start-publishing race.
- `409 Conflict` when the publication is orphaned because the platform was
  deleted; orphan history is read-only.
- `409 Conflict` when a scheduled WordPress post's computed `date_gmt` is not
  still in the future at publish time.
- `501 Not Implemented` when no provider adapter serves the platform type.
- `502 Bad Gateway` when the provider call fails.
- `500 Internal Server Error` when the external resource was created but the
  publication row could not be finalized. The publish finalization path does
  not delete provider resources, so the external resource id may require
  operator follow-up.

State conflicts are evaluated before content validation, so an
already-published or in-progress publication returns `409` even when the start
is in the past.

For YouTube rows, `thumbnailStatus` is:

- `NotConfigured` when no event thumbnail existed for the publish or the row is
  an unpublished YouTube projection.
- `Applied` when the stored event thumbnail was applied to the created YouTube
  broadcast.
- `Failed` when the broadcast was created but thumbnail application failed.

`Failed` does not make the publish eligible for a normal retry because the
external broadcast already exists. This feature does not include a retry route
or retry button. Operators recover by updating the thumbnail in YouTube Studio.
WordPress rows return `thumbnailStatus: null`.

## Delete Platform Publication

```text
DELETE /api/calendar-events/{calendarEventId}/platforms/{platformId}/publication
```

Deletes one completed platform publication from one calendar event. Both ids
come from the route and the request body is empty. The API deletes or confirms
the provider resource first, then conditionally deletes the local publication
row only when it is still a non-orphan `Published` row with the same
`externalResourceId`.

The route is allowed only for a future calendar event by backend UTC time, an
active platform, a `Published` publication with an `externalResourceId`, and a
secret-free target snapshot that still matches the active platform. The browser
must use the `canDeletePublication` flag from the calendar event details row
and must not re-derive eligibility from local time, status, or provider ids.

For a YouTube platform, the provider cleanup deletes the stored
`externalResourceId` as a YouTube `liveBroadcast` id. A YouTube not-found
result is success-equivalent because the requested provider state already
holds. A YouTube state conflict, such as a broadcast that cannot be deleted in
its current provider status, maps to `409 Conflict`.

For a WordPress platform, the provider cleanup treats `externalResourceId` as
the numeric WordPress post id, discovers the WordPress REST API root from the
configured site URL, and calls logical route `DELETE /wp/v2/posts/{id}` with
`force=true` using Basic Auth with the stored WordPress username and
Application Password. A WordPress not-found result is success-equivalent.
Authorization or other provider failures map to `502 Bad Gateway`.

Success response (`200 OK`) is the recomputed event-platform row, using the
same shape as `GET /api/calendar-events/{calendarEventId}`:

```json
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
```

Status codes:

- `200 OK` when provider cleanup succeeds, when the provider resource is
  already gone, or when the local row is already not published. The response is
  the recomputed event-platform row.
- `404 Not Found` when the calendar event id or active platform id does not
  exist.
- `409 Conflict` when the publication is orphaned, the event start is past or
  equal to backend UTC now, the row has no `externalResourceId`, the platform
  target no longer matches the stored snapshot, a publish is already in
  progress, the provider reports a state conflict, or the row changes before
  local deletion.
- `501 Not Implemented` when no provider cleanup adapter serves the platform
  type.
- `502 Bad Gateway` when provider cleanup fails. The local publication row is
  kept so the operator can recover and retry.

See [`../operations/platform-publication-cleanup.md`](../operations/platform-publication-cleanup.md)
for provider-specific cleanup behavior and recovery notes.
