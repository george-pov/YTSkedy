# Domain Vocabulary

Canonical product concepts and public identifier vocabulary for YTSkedy.

## Ownership

- Source of truth: durable domain terms, their meanings, and preferred public
  identifier vocabulary.
- Update when: adding or changing a domain concept or changing the default term
  used across API, application, persistence, UI, and integration boundaries.
- Validate with: compare touched contracts, code, and docs against this
  vocabulary and run `scripts/validate-docs.ps1`.

## Domain Vocabulary

- `calendar event`: Application-owned scheduling record created from user input
  and persisted by the API.
- `scheduled start`: Submitted local date-time plus explicit time-zone id.
- `scheduled start UTC`: UTC instant derived from a scheduled start and used for
  ordering and active calendar-event duplicate detection.
- `calendar event start defaults`: Independently optional application-wide
  weekday, local time, and time-zone values used only to initialize a newly
  opened calendar event create form.
- `start suggestion`: Advisory initial local date, local time, and time-zone
  values for a new calendar event. A complete suggestion is strictly future and
  not occupied by a stored event at read time; it is not a reservation.
- `event text fields`: The current application setting that defines the ordered
  text fields used by newly created calendar events.
- `event text field`: A configured field definition with a derived `fieldKey`,
  label, type, and max length.
- `event text value`: One submitted value for an event text field.
- `event text snapshot`: The field definitions and values stored on a calendar
  event when it is created. Stored events keep this snapshot after settings
  edits.
- `field key`: Backend-derived `textN` key for an event text field, such as
  `text1` or `text2`. Use `fieldKey` in HTTP and TypeScript shapes.
- `publish status`: Authoritative per-platform publication state represented by
  `PublishStatus`, such as `NotPublished`, `Publishing`, or `Published`.
- `publishing status`: Informational calendar-event list aggregate represented
  by `PublishingStatus` and exposed as `publicationStatus`. Values are
  `NotPublished`, `PartiallyPublished`, `FullyPublished`, and the reserved
  `Failed`. It is distinct from per-platform publish status and is not an action
  policy input. The UI labels the column `Publication Status`.
- `published platform id index`: Derived, secret-free set of successfully
  published platform ids stored with a calendar event for list aggregation.
  Platform publication rows remain authoritative.
- `action policy`: Pure domain rule object that computes write eligibility.
- `template`: Reusable free-text publishing content with placeholder tokens.
- `template type`: Provider family associated with a template, currently
  `YouTube` or `WordPress`.
- `template token`: Placeholder token available to template content, such as
  `text1`, `longDateEn`, or an active platform `referenceKey`.
- `publishing content`: Platform-owned title and description template selection
  used for provider publishing. Both template ids are required. It is not
  provider credentials or provider-specific options.
- `rendered publishing content`: Title and description text produced by the
  backend from a calendar event and platform publishing content. It is
  recalculated for unpublished previews and is transient before publish.
- `content snapshot`: Rendered title and description stored on a platform
  publication when publish starts. It records what YTSkedy sent, or attempted
  to send, to the provider.
- `broadcast`: YouTube `liveBroadcast` resource metadata, including title,
  description, visibility, made-for-kids state, and scheduled start.
- `publisher`: Application port (`IPlatformPublisher`) or provider adapter that
  creates an external provider resource for a platform type.
- `authorization policy`: API boundary rule that maps authenticated principals,
  scopes, roles, and resolved endpoints to an authorization result.
- `credentials`: Provider credential material needed by a platform. It may be
  secret-bearing and must be redacted from reads, logs, and snapshots.
- `platform`: Configured publishing destination such as a YouTube channel or a
  WordPress site.
- `reference key`: Optional user-managed lookup key on a configured platform.
  It is provider-neutral, unique case-insensitively when set, and preserves the
  entered casing for display. It can also be used as a template-token name that
  resolves to that platform publication's `externalResourceId` when the
  selected calendar event has a published row for the platform.
- `publish settings`: Provider-specific settings used when publishing through a
  platform. These settings may carry secrets and must be redacted where they
  cross read, logging, or snapshot boundaries.
- `platform publication`: Publish state and provider result for one calendar
  event on one platform.
- `provider`: Infrastructure adapter that performs an external publish for a
  platform type.
- `external resource id`: Provider-owned identifier returned after a publish.

Verify exact YouTube resource names, required fields, and API behavior against
official Google or YouTube documentation before implementation.

## Code Identifier Glossary

This glossary defines concepts and default vocabulary. It is not a mandatory
identifier template. A code identifier may use a shorter term when surrounding
context supplies the omitted words.

Prefer stable glossary terms for:

- public application, API, persistence, and integration-boundary types where
  namespace or containing type does not already supply the context
- externally visible request, response, command, result, and entity names
- names where both local application state and YouTube-owned state are nearby
- credential, token, time-zone, and scheduled-time concepts

Shorter names are acceptable for:

- local variables and parameters inside a clearly named method
- public types when the namespace, route, table, or containing type already
  supplies the omitted domain words
- private helper methods inside a clearly named type
- test variables where the test name already supplies the domain context
- DTO properties when the surrounding DTO name already supplies the context
- command, handler, repository, and result names when the command shape or route
  carries the missing object, such as a `PlatformId`

| Preferred term | Default use | Shorter or alternate form |
| --- | --- | --- |
| `CalendarEvent` | Application-owned calendar input or persisted scheduling row. | `Event` is acceptable inside calendar-event-specific code when it cannot be confused with a YouTube broadcast or .NET event. |
| `CalendarEventView` | Read model returned by calendar event query use cases. | `View` is acceptable inside calendar-event-specific mapping code. |
| `EventTextFields` | Current application setting containing ordered event text field definitions. | `Fields` is acceptable inside settings-specific code. |
| `EventTextField` | One configured event text field definition with `FieldKey`, `Label`, `Type`, and `MaxLength`. | `Field` is acceptable inside event-text-specific code. |
| `EventTextValue` | Submitted value for one event text field. | `Value` is acceptable inside snapshot-specific code. |
| `EventTextSnapshot` | Stored calendar-event field definitions and values. | `Text` is acceptable as a property name on calendar-event types when the containing type supplies the event context. |
| `FieldKey` | Derived `textN` identifier for an event text field. | Use `fieldKey` in HTTP and TypeScript shapes. Do not use `referenceKey` for event text fields. |
| `Template` | Reusable free-text publishing content with placeholder tokens. | Use `Template` directly. |
| `TemplateType` | Provider family associated with a template. | `Type` is acceptable inside template-specific code. |
| `TemplateView` | Read model for a stored template. | `View` is acceptable inside template-specific mapping code. |
| `TemplateToken` | Placeholder token available to template content. | `Token` is acceptable inside template-token-specific code. |
| `TemplateTokenCatalog` | Source of available template tokens from event text fields, fixed date tokens, and platform reference keys. | `Catalog` is acceptable inside template-token-specific code. |
| `Platform` | Configured publishing destination. | `Destination` is acceptable only for user-facing copy when it is clearer. |
| `ReferenceKey` | Optional provider-neutral lookup key on a configured platform. Uniqueness and lookup are case-insensitive while display casing is preserved. | Use `referenceKey` in HTTP and TypeScript shapes. Avoid provider-specific variants such as `YouTubeReferenceKey`. A matching template placeholder resolves to the platform publication `externalResourceId` only when that value is available. |
| `PublishingContent` | Platform-owned title and description template selection rendered before publishing. Both template ids are required. | Use `publishingContent` in HTTP and TypeScript shapes. Keep separate from `PublishSettings`. |
| `RenderedContent` | Title and description text rendered from a calendar event and platform publishing content. | Keep transient for unpublished rows. Persist as a `ContentSnapshot` only when publish starts. |
| `ContentSnapshot` | Rendered title and description stored on a platform publication. | Use inside platform-publication code where snapshot context is clear. |
| `PublishSettings` | Provider-specific settings used when publishing through a platform. May be secret-bearing. | Avoid `DefaultPublishingSettings`; create a new platform when settings differ. |
| `YouTubeSettings` | YouTube-specific publish settings used by a YouTube platform. | `PublishSettings` is acceptable inside YouTube-platform-specific code. |
| `PlatformPublication` | Publish state for one calendar event on one platform. | `Publication` is acceptable inside platform-specific namespaces, types, or tests. |
| `PublishStatus` | Status of a platform publication. | `Status` is acceptable inside platform-publication-specific code. |
| `PublishingStatus` | Informational aggregate of a calendar event's successful publications compared with active platforms. | Use `publicationStatus` in HTTP and TypeScript list shapes. Do not use it for per-platform rows or action policy. |
| `ExternalResourceId` | Provider-owned id returned after publishing. | `ResourceId` is acceptable inside provider-specific result mapping. Provider-specific ids such as `YouTubeBroadcastId` belong only at provider boundaries. |
| `ScheduledStart` | Local date/time plus explicit time zone context. | `StartDate` or `StartTime` is acceptable only when the value is truly date-only or time-only, or when the enclosing type already owns the scheduling context. |
| `ScheduledStartUtc` | Persisted UTC instant derived from the scheduled start. | `UtcStart` is acceptable in storage or formatting helpers. Avoid bare `Date` unless the local scope is very small and unambiguous. |
| `Broadcast` | YouTube live broadcast metadata concept. | Use mainly in YouTube-specific adapters and tests. |
| `YouTubeBroadcast` | YouTube `liveBroadcast` resource or adapter concept. | `Broadcast` is acceptable inside YouTube-specific adapters when the provider context is already clear. |
| `YouTubeBroadcastId` | Stored id of a YouTube `liveBroadcast` resource. | `BroadcastId` is acceptable inside YouTube-specific code. |
| `YouTubeCredentials` | Secret Google OAuth credentials for one YouTube platform. | Stored in the platform row. Never expose in HTTP reads, logs, or publication snapshots. |
| `Visibility` | YouTube visibility or privacy state. | `Privacy` is acceptable when mirroring YouTube field names or user-facing wording. |
| `AuthOptions` | API bearer-token validation and authorization configuration. | `Options` is acceptable inside auth composition code. |
| `AuthorizationPolicy` | API authorization rule that evaluates scopes, roles, and endpoints. | `Policy` is acceptable inside auth-specific tests and helpers. |
| `AuthorizationResult` | Authorization decision returned by `AuthorizationPolicy`. | `Result` is acceptable inside auth-specific code. |
| `Credentials` | Provider credential material for a platform. | Treat as secret-bearing unless the type explicitly exposes a redacted projection. |

Use `Id` suffixes for identifiers: `CalendarEventId`, `TemplateId`,
`YouTubeBroadcastId`, `ClientId`, `TenantId`, and `PlatformId`. A bare `Id` is
acceptable in DTOs or entities where the surrounding type supplies the resource.

