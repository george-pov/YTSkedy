# Configured Platforms HTTP Contract

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
- Carry the scope required by the endpoint (`CalendarEvents.Read` for normal
  platform `GET` routes, and `CalendarEvents.Write` for category lookup,
  `POST`, `PUT`, `DELETE`, publish, and publication delete). Wrong scope
  returns `403`.
- Carry the `CalendarEvents.Operator` app role in the `roles` claim. Missing
  role returns `403`.

Function keys are not part of authorization for these endpoints.

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
      "refreshTokenConfigured": true,
      "clientSecretDisplayValue": "*********A3B",
      "refreshTokenDisplayValue": "*********Z9Y"
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
    "titleTemplateId": "wordpress-title-template-id",
    "descriptionTemplateId": "wordpress-description-template-id"
  },
  "publishSettings": {
    "siteUrl": "https://blog.example.test/",
    "username": "publisher",
    "postStatus": "future",
    "sticky": true,
    "scheduleOffsetHours": 25,
    "categoryIds": [12, 34],
    "applicationPasswordConfigured": true,
    "passwordDisplayValue": "*******"
  }
}
```

- `platformId` is a server-generated 32-character lowercase hex GUID.
- `name` is required, trimmed, at most 50 characters, and globally unique
  across all platforms.
- `referenceKey` is optional. It is returned as `null` when unset. Non-empty
  keys are trimmed, must be 1 to 15 ASCII letters, digits, or hyphen, and must
  contain no spaces or underscores. Uniqueness is case-insensitive across all
  platforms, so `youTube1` and `youtube1` conflict. Responses preserve the
  stored display casing. A non-empty reference key is also available as a
  template token name for resolving that platform's published
  `externalResourceId` on the selected calendar event.
- `type` is `YouTube` or `WordPress`. It is set on create and is immutable
  because it determines the publish-settings schema and provider adapter.
- `publishingContent` is provider-neutral title and description template
  selection. It is required on create and update. Both `titleTemplateId` and
  `descriptionTemplateId` are required, non-blank template ids, and the
  selected templates must have the same provider family as the platform type.
  There is no `(none)` option and no direct fallback from event text fields.
  `publishingContent` stores only template ids; it does not copy template text
  and it is separate from provider-specific `publishSettings`.
- YouTube `publishSettings.credentials.clientId` is the Google OAuth client id.
  YouTube create and update requests can include
  `publishSettings.credentials.clientSecret` and
  `publishSettings.credentials.refreshToken`, but responses never return them.
  Responses return `clientSecretConfigured` and
  `refreshTokenConfigured` plus response-only display values instead.
  `clientSecretDisplayValue` and `refreshTokenDisplayValue` are exactly 12
  characters. When the stored value has at least three characters, the display
  value is nine `*` characters plus the last three stored characters. Shorter
  stored values display as 12 `*` characters. These display values hide the
  original length and are not accepted in create or update request bodies.
  `privacyStatus` is `private`, `public`, or `unlisted`;
  `selfDeclaredMadeForKids` defaults to `false` on create when omitted.
- WordPress `publishSettings.siteUrl` is the WordPress site root used for REST
  API discovery. Non-local site URLs must use HTTPS. `http://localhost` and
  `http://127.0.0.1` are allowed for local development only.
- WordPress `publishSettings.username` is the WordPress username used with an
  Application Password.
- WordPress `publishSettings.postStatus` is `draft`, `pending`, `private`,
  `future`, or `publish`. `future` is the API value for a scheduled WordPress
  post.
- WordPress `publishSettings.sticky` is optional on create and update and
  defaults to `false`.
- WordPress `publishSettings.scheduleOffsetHours` is required when
  `postStatus` is `future`, must be from `1` through `168`, and must be
  omitted or `null` for every other WordPress post status.
- WordPress `publishSettings.categoryIds` is required and non-null in every
  create request, update request, and response. `[]` is valid. Non-empty arrays
  must contain distinct positive integers and preserve submitted order. These
  are existing WordPress category term IDs; names and slugs are lookup data and
  are not stored in platform settings. Payloads and stored WordPress settings
  from the previous shape without `categoryIds` are not compatible with the
  current contract.
- WordPress create and update requests can include
  `publishSettings.applicationPassword`, but responses never return it.
  Responses return `applicationPasswordConfigured` and `passwordDisplayValue`
  instead. `passwordDisplayValue` is exactly seven `*` characters and reveals
  no stored characters. It confirms only that an Application Password is
  configured and is not accepted in create or update request bodies.

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
          "refreshTokenConfigured": true,
          "clientSecretDisplayValue": "*********A3B",
          "refreshTokenDisplayValue": "*********Z9Y"
        },
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
    "titleTemplateId": "wordpress-title-template-id",
    "descriptionTemplateId": "wordpress-description-template-id"
  },
  "publishSettings": {
    "siteUrl": "https://blog.example.test/",
    "username": "publisher",
    "applicationPassword": "<wordpress-application-password>",
    "postStatus": "future",
    "sticky": true,
    "scheduleOffsetHours": 25,
    "categoryIds": []
  }
}
```

Success returns `200 OK` with the created platform, including its generated
`platformId`. YouTube responses redact `clientSecret` and `refreshToken` and
return configured flags plus display values. WordPress responses redact
`applicationPassword` and return `applicationPasswordConfigured` plus
`passwordDisplayValue`.

Redacted display values are response-only. Do not send
`clientSecretDisplayValue`, `refreshTokenDisplayValue`, or
`passwordDisplayValue` in create request bodies.

Status codes:

- `200 OK` with the created platform.
- `400 Bad Request` when the body is missing or not valid JSON.
- `400 Bad Request` when `name` is empty or longer than 50 characters.
- `400 Bad Request` when `referenceKey` is non-empty and is longer than 15
  characters or contains any character other than ASCII letters, digits, or
  hyphen.
- `400 Bad Request` when `type` is not a recognized platform type.
- `400 Bad Request` when `publishingContent` is missing, either template id is
  missing or blank, or a referenced template does not exist for the selected
  platform type.
- `400 Bad Request` when `publishSettings` is missing.
- `400 Bad Request` for invalid YouTube settings: missing `credentials`,
  missing `credentials.clientId`, missing `credentials.clientSecret` on create,
  missing `credentials.refreshToken` on create, or `privacyStatus` not
  `private`, `public`, or `unlisted`.
- `400 Bad Request` for invalid WordPress settings: missing `siteUrl`, invalid
  or insecure `siteUrl`, missing `username`, missing `applicationPassword`, or
  `postStatus` not `draft`, `pending`, `private`, `future`, or `publish`.
- `400 Bad Request` when WordPress `categoryIds` is missing or `null`, or when
  it contains a non-positive or duplicate value.
- `400 Bad Request` when WordPress `postStatus` is `future` and
  `scheduleOffsetHours` is missing or outside `1..168`.
- `400 Bad Request` when WordPress `scheduleOffsetHours` is supplied for any
  non-`future` post status.
- `409 Conflict` when another platform already uses the same name.
- `409 Conflict` when another platform already uses the same non-empty
  `referenceKey`, compared case-insensitively.

## Update Platform

```text
PUT /api/platforms/{platformId}
```

Replaces the name, reference key, publishing content, and publish settings of
an existing platform. `type` is immutable and is not accepted in the update
body. The existing platform type selects the expected settings schema.

YouTube request body:

```json
{
  "name": "Main YouTube channel",
  "referenceKey": "main-youtube",
  "publishingContent": {
    "titleTemplateId": "youtube-title-template-id",
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
sending a blank string clears the stored key. Sending a non-empty value
replaces the stored key after trimming and the same validation and uniqueness
rules used by create.

Omitting `credentials.clientSecret` or `credentials.refreshToken`, or sending
either field blank, preserves the stored YouTube secret values. A non-blank
value replaces the stored value.

WordPress request body that preserves the stored Application Password:

```json
{
  "name": "Company blog",
  "referenceKey": "blog-1",
  "publishingContent": {
    "titleTemplateId": "wordpress-title-template-id",
    "descriptionTemplateId": "wordpress-description-template-id"
  },
  "publishSettings": {
    "siteUrl": "https://blog.example.test/",
    "username": "publisher",
    "postStatus": "future",
    "sticky": true,
    "scheduleOffsetHours": 25,
    "categoryIds": [12, 34]
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
    "postStatus": "publish",
    "sticky": false,
    "categoryIds": []
  }
}
```

For WordPress updates, omitting `applicationPassword` or sending it blank
preserves the stored Application Password. A non-blank value replaces it.
Include the current `publishingContent` ids in every update body when they
should be preserved. Success returns `200 OK` with the updated platform and
redacted settings.

Redacted display values are response-only. Do not send
`clientSecretDisplayValue`, `refreshTokenDisplayValue`, or
`passwordDisplayValue` in update request bodies. Use the actual replacement
secret value when replacing a stored secret.

Status codes:

- `200 OK` with the updated platform.
- `400 Bad Request` for a missing or invalid body, invalid name, invalid
  reference key, missing or invalid `publishingContent`, or invalid publish
  settings using the same type-specific rules as create. A YouTube update can
  omit or blank `credentials.clientSecret` and `credentials.refreshToken` to
  preserve the stored values. A WordPress update can omit or blank
  `applicationPassword` to preserve the stored value.
- `400 Bad Request` when `publishingContent` references a template that does
  not exist for the platform type.
- `404 Not Found` when no platform has the id.
- `409 Conflict` when renaming to a name already used by another platform.
- `409 Conflict` when changing to a non-empty `referenceKey` already used by
  another platform, compared case-insensitively.
- `409 Conflict` when the platform has a publication that is currently
  `Publishing`.

Platform-publication rows and provider publication delete behavior are
unchanged by `referenceKey`; publication rows continue to store
provider-neutral `externalResourceId` values, not platform reference keys.
During template rendering, an active platform's `referenceKey` can resolve to
that stored external resource id for the same calendar event.

## List WordPress Categories

```text
GET /api/platforms/{platformId}/wordpress/categories
```

Lists existing categories from the WordPress site configured on a saved
platform. The backend reads that platform's stored site URL, username, and
Application Password, discovers the WordPress REST API root, and performs the
provider request. Provider credentials are never returned to the browser. The
route is read-only at WordPress but requires `CalendarEvents.Write` because its
results configure later provider writes.

Optional query parameters:

- `search`: trimmed non-empty text of at most 100 characters.
- `includeIds`: one comma-separated list of distinct positive integer IDs.
- `page`: an integer at least `1`; default `1`.
- `pageSize`: an integer from `1` through `100`; default `25`.

`search` and `includeIds` cannot be combined. Repeating any query parameter is
invalid. With neither filter, WordPress returns the first name-ascending page.
The backend always requests `hide_empty=false` and maps WordPress paging
headers to the response.

Success response (`200 OK`):

```json
{
  "items": [
    {
      "id": 12,
      "name": "Events",
      "slug": "events"
    }
  ],
  "page": 1,
  "pageSize": 25,
  "total": 1,
  "totalPages": 1
}
```

Status codes:

- `200 OK` with the ordered category page, including `items: []` when no
  category matches.
- `400 Bad Request` for invalid, empty, repeated, mutually exclusive, or
  out-of-range query values.
- `404 Not Found` when the saved platform does not exist.
- `409 Conflict` when the saved platform is not a WordPress platform or its
  type and settings do not match.
- `502 Bad Gateway` when endpoint discovery, transport, provider status,
  provider JSON, category records, or paging metadata fails. The response is
  fixed and contains no provider detail or credentials.

The route never creates or mutates categories. It is unavailable for a new
unsaved platform because no stored provider settings exist yet.

The variable-only manual acceptance client is
`src/api/Test/Manual/wordpress-categories.http`. Keep bearer tokens, WordPress
credentials, real platform and event IDs, and raw provider responses out of the
tracked file.

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

## Related Contracts

- Platform publication state and actions:
  [`platform-publications.md`](platform-publications.md)
- Calendar events: [`calendar-events.md`](calendar-events.md)
- Templates: [`templates.md`](templates.md)
