# Platform Publishing HTTP Contract

Platform and platform-publishing endpoints are hosted by `YTSkedy.AzureFunctions`
under the Azure Functions `/api` prefix.

A `platform` is a configured publishing destination such as a YouTube channel
or WordPress site. A calendar event is a provider-neutral scheduling record and
carries no publish status of its own. Publish state lives in a
`platform publication`: the relationship between one calendar event and one
platform. Publishing always targets an explicit platform id; there is no
calendar-event-level publish route.

The implemented provider types are `YouTube` and `WordPress`.

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

`x-functions-key` is not accepted on these endpoints. Local manual checks
acquire a bearer token via the `az`-based recipe documented in
`docs/api/development/build-and-test.md`.

## Platform Shape

A YouTube platform is returned as:

```json
{
  "platformId": "4fb4a32f3f344de1a7c3a9f4a2f94918",
  "name": "Main YouTube channel",
  "referenceKey": "youTube1",
  "type": "YouTube",
  "publishingContent": {
    "titleTemplateId": "youtube-title-template-id",
    "descriptionTemplateId": "youtube-description-template-id"
  },
  "publishSettings": {
    "credentials": {
      "clientId": "google-oauth-client-id",
      "clientSecretConfigured": true,
      "refreshTokenConfigured": true
    },
    "privacyStatus": "private",
    "selfDeclaredMadeForKids": false
  }
}
```

A WordPress platform is returned as:

```json
{
  "platformId": "5aa4a32f3f344de1a7c3a9f4a2f94918",
  "name": "Company blog",
  "referenceKey": null,
  "type": "WordPress",
  "publishingContent": {
    "titleTemplateId": null,
    "descriptionTemplateId": null
  },
  "publishSettings": {
    "siteUrl": "https://blog.example.test/",
    "username": "publisher",
    "postStatus": "draft",
    "applicationPasswordConfigured": true
  }
}
```

- `platformId` is a server-generated 32-character lowercase hex GUID.
- `name` is required, trimmed, at most 50 characters, and globally unique across
  all platforms.
- `referenceKey` is optional. It is returned as `null` when unset. Non-empty
  keys are trimmed, must be 1 to 15 ASCII letters, digits, or hyphen, and must
  contain no spaces or underscores. Uniqueness is case-insensitive across all
  platforms, so `youTube1` and `youtube1` conflict. Responses preserve the
  stored display casing.
- `type` is `YouTube` or `WordPress`. It is set on create and is immutable
  because it determines the publish-settings schema and provider adapter.
- `publishingContent` is provider-neutral title and description template
  selection. `titleTemplateId` and `descriptionTemplateId` are template ids or
  `null`. A `null` id means the backend calculates that field from the calendar
  event when previewing or publishing. Omitting `publishingContent`, sending a
  `null` field, or sending a blank template id all mean `(none)`. The selected
  templates must have the same provider family as the platform type.
  `publishingContent` stores only template ids; it does not copy template text
  and it is separate from provider-specific `publishSettings`.
- YouTube `publishSettings.credentials.clientId` is the Google OAuth client id.
  YouTube create and update requests can include
  `publishSettings.credentials.clientSecret` and
  `publishSettings.credentials.refreshToken`, but responses never return them.
  Responses return `clientSecretConfigured` and `refreshTokenConfigured`
  instead. `privacyStatus` is `private`, `public`, or `unlisted`;
  `selfDeclaredMadeForKids` defaults to `false` on create when omitted.
- WordPress `publishSettings.siteUrl` is the WordPress site root.
  Non-local site URLs must use HTTPS. `http://localhost` and
  `http://127.0.0.1` are allowed for local development only.
- WordPress `publishSettings.username` is the WordPress username used with an
  Application Password.
- WordPress `publishSettings.postStatus` is `draft` or `publish`.
- WordPress create and update requests can include
  `publishSettings.applicationPassword`, but responses never return it.
  Responses return `applicationPasswordConfigured` instead.

## List Platforms

```text
GET /api/platforms
GET /api/platforms?type=YouTube
GET /api/platforms?type=WordPress
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
      "referenceKey": "youTube1",
      "type": "YouTube",
      "publishingContent": {
        "titleTemplateId": "youtube-title-template-id",
        "descriptionTemplateId": "youtube-description-template-id"
      },
      "publishSettings": {
        "credentials": {
          "clientId": "google-oauth-client-id",
          "clientSecretConfigured": true,
          "refreshTokenConfigured": true
        },
        "privacyStatus": "private",
        "selfDeclaredMadeForKids": false
      }
    },
    {
      "platformId": "5aa4a32f3f344de1a7c3a9f4a2f94918",
      "name": "Company blog",
      "referenceKey": null,
      "type": "WordPress",
      "publishingContent": {
        "titleTemplateId": null,
        "descriptionTemplateId": null
      },
      "publishSettings": {
        "siteUrl": "https://blog.example.test/",
        "username": "publisher",
        "postStatus": "draft",
        "applicationPasswordConfigured": true
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

YouTube request body:

```json
{
  "name": "Main YouTube channel",
  "referenceKey": "youTube1",
  "type": "YouTube",
  "publishingContent": {
    "titleTemplateId": "youtube-title-template-id",
    "descriptionTemplateId": "youtube-description-template-id"
  },
  "publishSettings": {
    "credentials": {
      "clientId": "<google-oauth-client-id>",
      "clientSecret": "<google-oauth-client-secret>",
      "refreshToken": "<google-oauth-refresh-token>"
    },
    "privacyStatus": "private",
    "selfDeclaredMadeForKids": false
  }
}
```

WordPress request body:

```json
{
  "name": "Company blog",
  "referenceKey": "blog-1",
  "type": "WordPress",
  "publishingContent": {
    "titleTemplateId": null,
    "descriptionTemplateId": "wordpress-description-template-id"
  },
  "publishSettings": {
    "siteUrl": "https://blog.example.test/",
    "username": "publisher",
    "applicationPassword": "<wordpress-application-password>",
    "postStatus": "draft"
  }
}
```

Success returns `200 OK` with the created platform, including its generated
`platformId`. YouTube responses redact `clientSecret` and `refreshToken` and
return configured flags. WordPress responses redact `applicationPassword` and
return `applicationPasswordConfigured`. If `publishingContent` is omitted, both
template ids default to `null`.

Status codes:

- `200 OK` with the created platform.
- `400 Bad Request` when the body is missing or not valid JSON.
- `400 Bad Request` when `name` is empty or longer than 50 characters.
- `400 Bad Request` when `referenceKey` is non-empty and is longer than 15
  characters or contains any character other than ASCII letters, digits, or
  hyphen.
- `400 Bad Request` when `type` is not a recognized platform type.
- `400 Bad Request` when `publishSettings` is missing.
- `400 Bad Request` when `publishingContent` references a template that does
  not exist for the selected platform type.
- `400 Bad Request` for invalid YouTube settings: missing `credentials`,
  missing `credentials.clientId`, missing `credentials.clientSecret` on create,
  missing `credentials.refreshToken` on create, or `privacyStatus` not
  `private`, `public`, or `unlisted`.
- `400 Bad Request` for invalid WordPress settings: missing `siteUrl`, invalid
  or insecure `siteUrl`, missing `username`, missing `applicationPassword`, or
  `postStatus` not `draft` or `publish`.
- `409 Conflict` when another platform already uses the same name.
- `409 Conflict` when another platform already uses the same non-empty
  `referenceKey`, compared case-insensitively.

## Update Platform

```text
PUT /api/platforms/{platformId}
```

Replaces the name and publish settings of an existing platform. `type` is
immutable and is not accepted in the update body. The existing platform type
selects the expected settings schema.

YouTube request body:

```json
{
  "name": "Main YouTube channel",
  "referenceKey": "main-youtube",
  "publishingContent": {
    "titleTemplateId": null,
    "descriptionTemplateId": "youtube-description-template-id"
  },
  "publishSettings": {
    "credentials": {
      "clientId": "<google-oauth-client-id>"
    },
    "privacyStatus": "unlisted",
    "selfDeclaredMadeForKids": false
  }
}
```

`referenceKey` is replace-style on update: omitting it, sending `null`, or
sending a blank string clears the stored key. Sending a non-empty value replaces
the stored key after trimming and the same validation and uniqueness rules used
by create.

Omitting `credentials.clientSecret` or `credentials.refreshToken`, or sending
either field blank, preserves the stored YouTube secret values. A non-blank
value replaces the stored value.

WordPress request body that preserves the stored Application Password:

```json
{
  "name": "Company blog",
  "referenceKey": "blog-1",
  "publishingContent": {
    "titleTemplateId": null,
    "descriptionTemplateId": null
  },
  "publishSettings": {
    "siteUrl": "https://blog.example.test/",
    "username": "publisher",
    "postStatus": "draft"
  }
}
```

WordPress request body that replaces the stored Application Password:

```json
{
  "name": "Company blog",
  "referenceKey": null,
  "publishingContent": {
    "titleTemplateId": "wordpress-title-template-id",
    "descriptionTemplateId": "wordpress-description-template-id"
  },
  "publishSettings": {
    "siteUrl": "https://blog.example.test/",
    "username": "publisher",
    "applicationPassword": "<replacement-wordpress-application-password>",
    "postStatus": "publish"
  }
}
```

For WordPress updates, omitting `applicationPassword` or sending it blank
preserves the stored Application Password. A non-blank value replaces it.
Omitting `publishingContent` sets both template ids to `null`; include the
current ids in the update body when they should be preserved. Success returns
`200 OK` with the updated platform and redacted settings.

Status codes:

- `200 OK` with the updated platform.
- `400 Bad Request` for a missing or invalid body, invalid name, or invalid
  reference key, or invalid publish settings using the same type-specific rules
  as create. A YouTube update can omit or blank `credentials.clientSecret` and
  `credentials.refreshToken` to preserve the stored values. A WordPress update
  can omit or blank `applicationPassword` to preserve the stored value.
- `400 Bad Request` when `publishingContent` references a template that does
  not exist for the platform type.
- `404 Not Found` when no platform has the id.
- `409 Conflict` when renaming to a name already used by another platform.
- `409 Conflict` when changing to a non-empty `referenceKey` already used by
  another platform, compared case-insensitively.
- `409 Conflict` when the platform has a publication that is currently
  `Publishing`.

Platform-publication rows, provider publish behavior, and provider publication
delete behavior are unchanged by `referenceKey`; publication rows continue to
store provider-neutral `externalResourceId` values, not platform reference
keys.

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
details response with `platformDeletedUtc` set, `canPublish: false`, and
`canDeletePublication: false`.

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
platform's `publishingContent` and the calendar event. A selected title template
or description template is rendered with the approved template tokens. A `null`
template id uses the event's English title or optional English description. The
rendered title and description are stored as a content snapshot when the
publication row enters `Publishing`.

For a YouTube platform this creates a scheduled YouTube `liveBroadcast` using
the rendered title and optional rendered description, the stored UTC scheduled
start, and the platform's `privacyStatus` and `selfDeclaredMadeForKids`. The
created broadcast id is returned as the provider-neutral `externalResourceId`.

For a WordPress platform this creates a post through
`POST /wp-json/wp/v2/posts` using Basic Auth with the configured WordPress
username and Application Password. The request maps the rendered title to
`title`, the optional rendered description to `content`, and the platform's
`postStatus` to `status`. The numeric WordPress post id is returned as the
provider-neutral `externalResourceId`.

YouTube success response (`200 OK`):

```json
{
  "platformId": "4fb4a32f3f344de1a7c3a9f4a2f94918",
  "platformName": "Main YouTube channel",
  "platformType": "YouTube",
  "status": "Published",
  "externalResourceId": "abc123youtubeid",
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
- `400 Bad Request` when the event start is not in the future.
- `404 Not Found` when the calendar event id or the platform id does not exist.
- `409 Conflict` when rendered publishing content is invalid. Invalid content
  includes a missing selected template, an empty rendered title, or an
  unresolved well-formed placeholder such as `{{ missingToken }}`.
- `409 Conflict` when the publication is already `Published`.
- `409 Conflict` when a publish is already in progress (`Publishing`),
  including when a concurrent request wins the start-publishing race.
- `409 Conflict` when the publication is orphaned because the platform was
  deleted; orphan history is read-only.
- `501 Not Implemented` when no provider adapter serves the platform type.
- `502 Bad Gateway` when the provider call fails.
- `500 Internal Server Error` when the external resource was created but the
  publication row could not be finalized. The publish finalization path does
  not delete provider resources, so the external resource id may require
  operator follow-up.

State conflicts are evaluated before content validation, so an
already-published or in-progress publication returns `409` even when the start
is in the past.

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
must use the `canDeletePublication` flag from the calendar event details row and
must not re-derive eligibility from local time, status, or provider ids.

For a YouTube platform, the provider cleanup deletes the stored
`externalResourceId` as a YouTube `liveBroadcast` id. A YouTube not-found result
is success-equivalent because the requested provider state already holds. A
YouTube state conflict, such as a broadcast that cannot be deleted in its
current provider status, maps to `409 Conflict`.

For a WordPress platform, the provider cleanup treats `externalResourceId` as
the numeric WordPress post id and calls
`DELETE /wp-json/wp/v2/posts/{id}?force=true` with Basic Auth using the stored
WordPress username and Application Password. A WordPress not-found result is
success-equivalent. Authorization or other provider failures map to
`502 Bad Gateway`.

Success response (`200 OK`) is the recomputed event-platform row, using the
same shape as `GET /api/calendar-events/{calendarEventId}`:

```json
{
  "platformId": "4fb4a32f3f344de1a7c3a9f4a2f94918",
  "platformName": "Main YouTube channel",
  "platformType": "YouTube",
  "status": "NotPublished",
  "externalResourceId": null,
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

## Manual Checks

Manual `.http` checks live under:

```text
src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/Platforms/
src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/CalendarEvents/
```

Platform CRUD and platform delete checks are in the `Platforms/` folder. Event
platform listing, platform-aware publish checks, and platform-publication delete
checks are in the `CalendarEvents/` folder because they hang off the
calendar-event route.

Before sending local requests:

- Reset the application-owned tables for the feature environment when starting
  from a clean state. See the reset note in
  `src/api/Test/YTSkedy.AzureFunctions.IntegrationTest/Platforms/ResetFeatureData.http`.
- Start Azurite or provide an Azure Storage connection string.
- For YouTube publish checks, create a Google OAuth refresh token and put the
  YouTube client id, client secret, and refresh token only in
  `http-client.env.json.user`. See
  [`../operations/youtube-publish-setup.md`](../operations/youtube-publish-setup.md).
- For WordPress publish checks, create a WordPress Application Password and put
  the WordPress username, Application Password, and site URL only in
  `http-client.env.json.user`.
- Start the Azure Functions host.
- Select the `local` environment in the `.http` editor and set tokens in
  `http-client.env.json.user`.

Keep deployed URLs, bearer access tokens, WordPress usernames, WordPress
Application Passwords, and personal values in `http-client.env.json.user`, not
in tracked environment files.
