# Templates HTTP Contract

Template endpoints are hosted by `YTSkedy.AzureFunctions` under the Azure
Functions `/api` prefix. A template is reusable free-text publishing content
with placeholder tokens (for example `{{ longDateEn }}` or `{{ text1 }}`) that
platform publishing content renders from calendar-event data and platform
publication state. The `Templates` page in the Angular UI (`/templates`)
consumes the list, create, update, and delete endpoints through a typed
`TemplatesService`; the `template-tokens` endpoint is available to the client
but is not yet surfaced in the editor.

## Authorization

Template HTTP triggers run at `AuthorizationLevel.Anonymous`, behind the same
worker-side bearer-token boundary as the calendar event endpoints. Templates
reuse the existing scopes; no new scope was added.

Every call must:

- Present a Microsoft Entra External ID access token via
  `Authorization: Bearer <token>`. Missing, invalid, expired, wrong-audience,
  or wrong-issuer tokens return `401`.
- Carry the scope required by the endpoint (`CalendarEvents.Read` for `GET`,
  `CalendarEvents.Write` for `POST`, `PUT`, and `DELETE`). Wrong scope returns
  `403`.
- Carry the `CalendarEvents.Operator` app role in the `roles` claim. Missing
  role returns `403`.

## Template Identity And Fields

- `id`: server-generated GUID rendered as a 32-character lowercase hex string
  (for example `9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d`). The id is stable across
  renames and is both `RowKey`-safe and URL-path-safe.
- `type`: `YouTube` or `WordPress` (case-insensitive on input). The type is
  immutable after create because it drives storage partitioning; changing type
  is a delete plus create.
- `name`: required, editable label. Non-empty and at most 50 characters.
  Unique within a type using an ordinal comparison.
- `content`: required free text. Non-empty and at most 2000 characters. Tokens
  are stored as-is and are not validated against the catalog by template writes.

## List Templates

```text
GET /api/templates
GET /api/templates?type={type}
```

Returns all templates, optionally filtered to one type. Requires the
`CalendarEvents.Read` scope.

Query parameters (optional):

- `type`: `YouTube` or `WordPress` (case-insensitive). When omitted, templates
  of every type are returned. The returned order is not significant.

Success response (`200 OK`):

```json
{
  "templates": [
    {
      "id": "9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d",
      "name": "Weeknight stream",
      "type": "YouTube",
      "content": "Live on {{ longDateEn }}: {{ text1 }}"
    }
  ]
}
```

Each item carries its `id` and `type`, so a client always has what the update
and delete routes need.

Current invalid query behavior:

- A `type` outside `YouTube`/`WordPress`, an empty `type`, or a repeated `type`
  value returns `400 Bad Request`.

## Create Template

```text
POST /api/templates
```

Creates a template. Requires the `CalendarEvents.Write` scope.

Request body:

```json
{
  "name": "Weeknight stream",
  "type": "YouTube",
  "content": "Live on {{ longDateEn }}: {{ text1 }}"
}
```

Success response (`200 OK`) carries the new id:

```json
{
  "id": "9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d",
  "name": "Weeknight stream",
  "type": "YouTube"
}
```

Current behavior and error mapping:

- Invalid JSON returns `400 Bad Request` (`Request body must be valid JSON.`).
- Missing request body returns `400 Bad Request` (`Request body is required.`).
- An empty `name` or a `name` longer than 50 characters returns
  `400 Bad Request` (`Name must be non-empty and at most 50 characters.`).
- An empty `content` or a `content` longer than 2000 characters returns
  `400 Bad Request` (`Content must be non-empty and at most 2000 characters.`).
- A `type` outside `YouTube`/`WordPress` returns `400 Bad Request`
  (`Template type must be 'YouTube' or 'WordPress'.`).
- A `name` already used within the type returns `409 Conflict`
  (`A {type} template named '{name}' already exists.`). The uniqueness check is
  check-then-write.

## Update Template

```text
PUT /api/templates/{type}/{id}
```

Replaces the `name` and `content` of an existing template located by `type` and
`id`. Requires the `CalendarEvents.Write` scope. The `type` is immutable and
travels in the route, so the body carries only `name` and `content`.

Request body:

```json
{
  "name": "Weeknight stream (edited)",
  "content": "Live on {{ shortDateEn }}: {{ text1 }}"
}
```

Success response (`200 OK`):

```json
{
  "id": "9f8b1c2d3e4f4a5b6c7d8e9f0a1b2c3d",
  "name": "Weeknight stream (edited)",
  "type": "YouTube"
}
```

Current behavior and error mapping:

- An unknown `{type}/{id}` returns `404 Not Found`
  (`Template '{id}' was not found.`).
- A `type` outside `YouTube`/`WordPress` returns `400 Bad Request`.
- An invalid `name` or `content` returns `400 Bad Request` with the same
  wording as create.
- Renaming to a `name` already used by another template in the type returns
  `409 Conflict` (`A {type} template named '{name}' already exists.`).
- Invalid JSON or a missing body returns `400 Bad Request`.
- The write is last-write-wins on `name` and `content`; the `type`, `id`, and
  the stored created timestamp are unchanged.

## Delete Template

```text
DELETE /api/templates/{type}/{id}
```

Deletes a template located by `type` and `id`. Requires the
`CalendarEvents.Write` scope. The request body is empty.

Success returns `204 No Content` with an empty body.

Current behavior and error mapping:

- An unknown `{type}/{id}` returns `404 Not Found`
  (`Template '{id}' was not found.`).
- A `type` outside `YouTube`/`WordPress` returns `400 Bad Request`.
- A template referenced by an active platform's `publishingContent` returns
  `409 Conflict` (`Template '{id}' is referenced by an active platform.`).
- The delete is a hard delete: removed templates are not recoverable.
  Tombstones, recycle-bin behavior, and restore are out of scope.

## List Template Tokens

```text
GET /api/template-tokens
```

Returns the placeholder tokens a client can offer for template content.
Requires the `CalendarEvents.Read` scope. The catalog combines the current
event text fields setting, fixed date tokens, and active platform reference
keys:

- `text1`, `text2`, `text3`, and so on, derived from the current field list
  order.
- `longDateEn`: Event submitted local date formatted with `en-US` as
  `MMMM d, yyyy`.
- `shortDateEn`: Event submitted local date formatted as `yyyy-MM-dd`.
- `longDateRu`: Event submitted local date formatted with `ru-RU` as
  `d MMMM yyyy`.
- `shortDateRu`: Event submitted local date formatted as `dd.MM.yyyy`.
- `longDateFr`: Event submitted local date formatted with `fr-FR` as
  `d MMMM yyyy`.
- `shortDateFr`: Event submitted local date formatted as `dd/MM/yyyy`.
- Active platform `referenceKey` values, such as `privateYouTube`.

The text token list reflects the current field setting for future template
authoring. During preview and publish, token values come from the selected
calendar event's stored text snapshot, so later settings edits do not reshape
existing events. During preview and publish, a platform reference-key token is
resolved from the selected calendar event's `Published` publication row for
the active platform with that `referenceKey`; the token value is that row's
`externalResourceId`. If no active platform has that key, or the matching
platform has no published external resource id for the selected event, the
placeholder is left unchanged.

Success response (`200 OK`) with the default field list and one active
platform reference key:

```json
{
  "tokens": [
    { "name": "text1" },
    { "name": "text2" },
    { "name": "longDateEn" },
    { "name": "shortDateEn" },
    { "name": "longDateRu" },
    { "name": "shortDateRu" },
    { "name": "longDateFr" },
    { "name": "shortDateFr" },
    { "name": "privateYouTube" }
  ]
}
```

A token `name` is the identifier without the surrounding `{{ }}` braces.
Template writes store unknown tokens as text; preview leaves unresolved
well-formed placeholders visible, while publish rejects unresolved well-formed
placeholders before any provider call. This includes reference-key placeholders
whose published external resource id is not available yet.

## Persistence

Templates persist in a dedicated `Templates` Azure Table, separate from the
calendar event table and bound through a keyed `TableClient`. The partition key
is derived from the type (`templates-youtube` or `templates-wordpress`) and the
row key is the GUID id. See [`../persistence.md`](../persistence.md).

## Manual Checks

These endpoints have no tracked `.http` checks yet. To exercise them locally:

- Start Azurite or provide an Azure Storage connection string.
- Start the Azure Functions host.
- Acquire a bearer token via the `az`-based recipe documented in
  [`../development/build-and-test.md`](../development/build-and-test.md) and
  send it as `Authorization: Bearer <token>`.
- Use the host port from the Azure Functions launch profile. The current local
  default is `http://localhost:7087`.
