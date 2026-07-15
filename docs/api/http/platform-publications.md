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

Publication status values are:

- `NotPublished`: no publication row exists for the pair.
- `Publishing`: one request owns the transient conditional-write guard.
- `Published`: required provider work and local finalization succeeded.
- `Failed`: a caught started non-cancellation failure was recorded for operator
  verification and explicit retry. A known provider id may be present.

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
`Failed`, `Published`, and orphan `Published` rows with a content snapshot, the
endpoint returns the stored snapshot.

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
- `200 OK` with `kind: "Snapshot"` for publishing, failed, published, or
  orphan published rows with stored content snapshots.
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

For a YouTube platform, the backend creates a scheduled YouTube
`liveBroadcast` as private using the rendered title, optional rendered
description, stored UTC scheduled start, and
`selfDeclaredMadeForKids`. The broadcast id also identifies its matching video
resource.

The backend then determines whether the configured `categoryId`,
`containsSyntheticMedia`, or final `privacyStatus` requires `videos.update`.
Null category, false disclosure, and private final privacy require no video
read or update. Otherwise it calls `videos.list` once for only the mutable parts
that will be updated: `snippet` for category, `status` for disclosure or a
privacy transition, or both. It copies the current mutable values into a new
update body, overrides the YTSkedy-owned values, and sends
`containsSyntheticMedia` explicitly whenever `status` is included. Public and
unlisted broadcasts remain private until this required update succeeds.

Only then does YTSkedy mark the local row `Published` and return the broadcast
id as the provider-neutral `externalResourceId`. When the calendar event has a
stored thumbnail, the backend applies it after the row is published. Thumbnail
application is a separate best-effort YouTube API call and does not change the
publication status.

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

A failed attempt appears on the next calendar event details read as a row such
as:

```json
{
  "platformId": "4fb4a32f3f344de1a7c3a9f4a2f94918",
  "platformName": "Main YouTube channel",
  "platformType": "YouTube",
  "status": "Failed",
  "externalResourceId": "abc123youtubeid",
  "thumbnailStatus": "NotConfigured",
  "publishedUtc": null,
  "platformDeletedUtc": null,
  "canPublish": true,
  "canDeletePublication": false,
  "canPreviewPublishingContent": true
}
```

`externalResourceId` is null when the provider failed before returning an id.
It is retained when a later required step failed after resource creation.

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
- `502 Bad Gateway` when a caught started non-cancellation failure is recorded
  as `Failed`. The response body is the fixed secret-safe message `Publishing
  failed. Verify the event on the publishing platform and delete it if
  necessary before retrying.`
- `500 Internal Server Error` when neither `Published` nor fallback `Failed`
  can be recorded. High-severity telemetry includes the known external id when
  available. The finalization path does not delete provider resources, so the
  operator must reconcile the provider directly.

State conflicts are evaluated before content validation, so an
already-published or in-progress publication returns `409` even when the start
is in the past. An active future `Failed` row is eligible for retry. Starting
the retry conditionally replaces that row with `Publishing`; only one
concurrent retry can reach the provider. Before retrying, the operator must
verify the event on the publishing platform and delete any uncertain provider
resource when necessary. YTSkedy does not automatically delete provider
resources after publish failure.

Request cancellation propagates immediately without a detached cleanup or
fallback state write. Cancellation after a provider write can therefore leave
a stale `Publishing` row or an unrecorded provider resource that requires
manual reconciliation.

For YouTube rows, `thumbnailStatus` is:

- `NotConfigured` when no event thumbnail existed for the publish or the row is
  an unpublished YouTube projection.
- `Applied` when the stored event thumbnail was applied to the created YouTube
  broadcast.
- `Failed` when the broadcast was created but thumbnail application failed.

`thumbnailStatus: "Failed"` is different from publication `status: "Failed"`.
A thumbnail failure leaves publication status `Published` and does not add a
publish retry. Operators recover by updating the thumbnail in YouTube Studio.
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
secret-free target snapshot that still matches the active platform. A `Failed`
row cannot use this route; the operator handles an uncertain provider resource
directly before retrying. The browser must use the `canDeletePublication` flag
from the calendar event details row and must not re-derive eligibility from
local time, status, or provider ids. The flag does not pre-evaluate target
snapshot mismatch; the backend always performs that final guard.

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

Target mismatch uses this structured `409 Conflict` response:

```json
{
  "code": "publication_target_mismatch",
  "message": "YTSkedy cannot delete this publication because the platform settings no longer match the target used to create it. Restore the original platform target and try again."
}
```

Other publication-delete conflicts retain their existing response shapes.

See [`../operations/platform-publication-cleanup.md`](../operations/platform-publication-cleanup.md)
for provider-specific cleanup behavior and recovery notes.
