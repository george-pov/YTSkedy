# Naming Guidance

This document defines domain vocabulary and naming rules for YTSkedy code and
tests.

## Ownership

- Source of truth: durable domain vocabulary, naming rules, and code identifier
  glossary for YTSkedy.
- Update when: adding a durable domain concept, changing preferred terminology,
  adding a provider concept that crosses boundaries, or changing backend naming
  enforcement guidance.
- Validate with: compare touched code and docs against the vocabulary, verify
  references when adding external guidance, run `git diff --check`, and run
  `dotnet format` only when `.editorconfig` naming rules change.

## Scope

Domain vocabulary in this document applies across backend API code, frontend UI
code, tests, and durable documentation. Detailed C# naming rules apply to the
backend projects under `src/api/`. Frontend TypeScript and Angular naming
should follow Angular conventions until a dedicated frontend naming section is
added.

## Source Priority

Apply guidance in this order:

1. Microsoft .NET and C# naming, async, options, dependency injection, and unit
   testing guidance.
2. YTSkedy domain vocabulary from this document.
3. External API vocabulary where the code crosses an integration boundary.
4. Clean-code readability heuristics such as intention revealing,
   pronounceable, searchable, and consistent names.

The current target is `net10.0`. .NET 10 and C# 14 do not require a new naming
style, so use modern Microsoft .NET conventions and keep names shaped by the
domain rather than by framework mechanics.

Non-Microsoft sources inform readability and unit-test naming heuristics only.
When secondary guidance conflicts with Microsoft .NET guidance, Microsoft
guidance wins.

## Length Rules

Length thresholds are review prompts for new and touched code, not strict
gates. Do not abbreviate meaningful domain terms only to satisfy a threshold,
and do not rename stable code only to satisfy a threshold unless the rename is
already in scope.

| Identifier | Target | Discuss above | Avoid above |
| --- | ---: | ---: | ---: |
| Class, record, struct, enum | 24 chars | 28 chars | 32 chars |
| Interface | 26 chars | 30 chars | 34 chars |
| Method | 22 chars | 28 chars | 36 chars |
| Property or field | 22 chars | 28 chars | 36 chars |
| Parameter or local variable | 20 chars | 28 chars | 36 chars |
| Unit test method | 60 chars | 80 chars | 100 chars |

When a name exceeds the discuss threshold, first remove repeated context before
accepting the long name. Prefer moving context into a namespace, containing type,
route, table partition, or method over adding more words to the identifier.

If a type name needs more than four concept words, move context into the
namespace or split the responsibility.

Prefer:

```csharp
namespace YTSkedy.Infrastructure.CalendarEvents;

public sealed class AzureCalendarEventRepository
{
}
```

Avoid:

```csharp
public sealed class AzureTableStorageCalendarEventRepositoryImplementation
{
}
```

## C# Naming Rules

- Use standard ASCII identifiers in repository code unless an external contract
  requires otherwise.
- Identifiers must start with a letter or underscore and should contain only
  letters, digits, and underscores.
- Do not use C# keywords as identifiers, even with the `@` escape.
- Do not use two consecutive underscores.
- Use `PascalCase` for namespaces, types, public members, and record
  positional parameters.
- Use `camelCase` for method parameters, class and struct primary constructor
  parameters, local variables, and local functions when they are not public API.
- Prefix interfaces with `I`.
- Prefix descriptive generic type parameters with `T`, such as `TCommand` or
  `TResult`. Use a single `T` only when the meaning is obvious.
- Use `Id`, `Utc`, `Json`, `Url`, `Api`, and `OAuth` consistently.
- Avoid abbreviations unless they are common in .NET, HTTP, OAuth, JSON, UTC,
  or a documented external API.
- Avoid Hungarian notation, `C` prefixes for classes, and `Enum` suffixes for
  enums.
- Avoid underscores in production code names except for private fields and
  unit test method separators.
- Use `_camelCase` for private instance fields when explicit fields are needed.
- Do not require `s_` or `t_` prefixes for static or thread-static fields.
  Prefer clear names that match the surrounding type's existing style.
- Do not mechanically enforce private static or thread-static field naming in
  `.editorconfig`.
- Use `PascalCase` for constants.
- Use `PascalCase` for events.
- Name classes, records, structs, and enums with nouns or noun phrases.
- Name methods with verbs or verb phrases.
- Name properties with nouns, noun phrases, or affirmative booleans such as
  `IsEnglish`, `CanPublish`, `CanUpdate`, or `CanDelete`.
- Name collection properties with plural nouns such as `Descriptions`, not
  `DescriptionList`.
- Async methods that return `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>`
  must end with `Async`.
- Methods that accept cancellation should name the parameter
  `cancellationToken`.
- Options classes should end in `Options`, such as `AuthOptions`.
- Service registration extension methods should use `Add{Service}` when this
  project later exposes reusable registration methods.

## Meaningful Names

Names should reveal intent without a comment. If the reader needs a comment to
know why the symbol exists, improve the name or the model.

Prefer the shortest name that is unambiguous at the declaration site and common
call sites. Let namespace, folder, containing type, route, table, or method
context carry repeated domain words. Use a longer name only when two shorter
names would collide in the same scope or make call sites unclear.

- Use one word per concept. Do not mix `Fetch`, `Retrieve`, and `Get` for the
  same operation.
- Keep names pronounceable and searchable.
- Avoid repeating context from the namespace, containing type, endpoint, table,
  or route.
- Avoid names that differ only by small qualifier changes.
- Avoid generic class words such as `Manager`, `Processor`, `Data`, `Info`,
  `Helper`, and `Util` unless a clearer domain or role name is not available.
- Prefer a named value object, command, request, or options type when a method
  needs more than three independent values.
- Use static factory methods when overloaded constructors would hide what the
  arguments mean.
- Keep command methods and query methods separate. A method named `Get` should
  not create, update, delete, bind, or schedule external resources.
- Review names at call sites, not only at declarations. A shorter name is
  acceptable when the call site stays clear and the type remains searchable.

## Operation Verbs

Use these verbs consistently.

| Verb | Meaning |
| --- | --- |
| `Get` | Return an existing value or cheap local result. No external write. |
| `Find` | Search for an optional local or persisted value. Nullable or option-like result is expected. |
| `List` | Return multiple existing items. |
| `Create` | Create a local or external resource. |
| `Update` | Replace or change an existing local record. |
| `Publish` | Create or update an externally visible provider resource from an application-owned record. |
| `Reserve` | Move a local record into an in-progress state before an external write. |
| `Mark` | Record a completed state transition, such as `MarkPublishedAsync`. |
| `Release` | Undo an in-progress local attempt after a failed external write. |
| `Validate` | Check rules and report failures. No mutation. |
| `Map` | Convert between API, application, domain, or persistence models. |
| `Parse` | Convert text into a typed value and fail on invalid input. Use `TryParse` when returning a success flag. |
| `Load` | Read from persistence, filesystem, or configuration. |
| `Save` | Persist an application-owned record. |
| `Delete` | Remove a resource when the external API or persistence API uses delete semantics. |
| `Handle` | Execute an application command through a handler. |

Use YouTube API verbs such as `Insert`, `Update`, `Delete`, `Bind`, and
`Transition` only in YouTube adapter code where matching the external contract
helps the reader. Application code should prefer product verbs.

Name externally visible writes with the resource being changed. For example,
prefer `CreateCalendarEventAsync`, `UpdateCalendarEventAsync`,
`DeleteCalendarEventAsync`, or `PublishCalendarEventAsync` over vague names
such as `ProcessAsync` or `SubmitAsync`.

## Domain Vocabulary

- `calendar event`: Application-owned scheduling record created from user input
  and persisted by the API.
- `scheduled start`: Submitted local date-time plus explicit time-zone id.
- `scheduled start UTC`: UTC instant derived from a scheduled start and used for
  ordering and active calendar-event duplicate detection.
- `localized description`: Calendar event title and optional description for one
  language code.
- `publish status`: Platform-publication state such as `NotPublished`,
  `Publishing`, or `Published`.
- `action policy`: Pure domain rule object that computes write eligibility.
- `template`: Reusable free-text publishing content with placeholder tokens.
- `template type`: Provider family associated with a template, currently
  `YouTube` or `WordPress`.
- `template token`: Code-defined placeholder token available to template
  content, such as `localizedDate`.
- `broadcast`: YouTube `liveBroadcast` resource metadata, including title,
  description, visibility, made-for-kids state, and scheduled start.
- `publisher`: Application port (`IPlatformPublisher`) or provider adapter that
  creates an external provider resource for a platform type.
- `authorization policy`: API boundary rule that maps authenticated principals,
  scopes, roles, and resolved endpoints to an authorization result.
- `credentials`: Provider credential material needed by a platform. It may be
  secret-bearing and must be redacted from reads, logs, and snapshots.
- `platform`: Configured publishing destination such as a YouTube channel or a
  future WordPress site.
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
| `LocalizedDescription` | Calendar event title and optional description for one language. | `Description` is acceptable inside calendar-event-specific code when the language context is clear. |
| `Template` | Reusable free-text publishing content with placeholder tokens. | Use `Template` directly. |
| `TemplateType` | Provider family associated with a template. | `Type` is acceptable inside template-specific code. |
| `TemplateView` | Read model for a stored template. | `View` is acceptable inside template-specific mapping code. |
| `TemplateToken` | Placeholder token available to template content. | `Token` is acceptable inside template-token-specific code. |
| `TemplateTokenCatalog` | Code-defined source of available template tokens. | `Catalog` is acceptable inside template-token-specific code. |
| `Platform` | Configured publishing destination. | `Destination` is acceptable only for user-facing copy when it is clearer. |
| `PublishSettings` | Provider-specific settings used when publishing through a platform. May be secret-bearing. | Avoid `DefaultPublishingSettings`; create a new platform when settings differ. |
| `YouTubeSettings` | YouTube-specific publish settings used by a YouTube platform. | `PublishSettings` is acceptable inside YouTube-platform-specific code. |
| `PlatformPublication` | Publish state for one calendar event on one platform. | `Publication` is acceptable inside platform-specific namespaces, types, or tests. |
| `PublishStatus` | Status of a platform publication. | `Status` is acceptable inside platform-publication-specific code. |
| `ExternalResourceId` | Provider-owned id returned after publishing. | `ResourceId` is acceptable inside provider-specific result mapping. Provider-specific ids such as `YouTubeBroadcastId` belong only at provider boundaries. |
| `ScheduledStart` | Local date/time plus explicit time zone context. | `StartDate` or `StartTime` is acceptable only when the value is truly date-only or time-only, or when the enclosing type already owns the scheduling context. |
| `ScheduledStartUtc` | Persisted UTC instant derived from the scheduled start. | `UtcStart` is acceptable in storage or formatting helpers. Avoid bare `Date` unless the local scope is very small and unambiguous. |
| `Broadcast` | YouTube live broadcast metadata concept. | Use mainly in YouTube-specific adapters and tests. |
| `YouTubeBroadcast` | YouTube `liveBroadcast` resource or adapter concept. | `Broadcast` is acceptable inside YouTube-specific adapters when the provider context is already clear. |
| `YouTubeBroadcastId` | Stored id of a YouTube `liveBroadcast` resource. | `BroadcastId` is acceptable inside YouTube-specific code. |
| `YouTubeCredentials` | Secret Google OAuth credentials for one YouTube platform. | Stored in the platform row until provider secrets move to the app-managed secret store. Never expose in HTTP reads, logs, or publication snapshots. |
| `Visibility` | YouTube visibility or privacy state. | `Privacy` is acceptable when mirroring YouTube field names or user-facing wording. |
| `AuthOptions` | API bearer-token validation and authorization configuration. | `Options` is acceptable inside auth composition code. |
| `AuthorizationPolicy` | API authorization rule that evaluates scopes, roles, and endpoints. | `Policy` is acceptable inside auth-specific tests and helpers. |
| `AuthorizationResult` | Authorization decision returned by `AuthorizationPolicy`. | `Result` is acceptable inside auth-specific code. |
| `Credentials` | Provider credential material for a platform. | Treat as secret-bearing unless the type explicitly exposes a redacted projection. |

Use `Id` suffixes for identifiers: `CalendarEventId`, `TemplateId`,
`YouTubeBroadcastId`, `ClientId`, `TenantId`, and `PlatformId`. A bare `Id` is
acceptable in DTOs or entities where the surrounding type supplies the resource.

## Layer Naming

Keep layer roles explicit but short.

| Layer | Pattern | Example |
| --- | --- | --- |
| Domain | Domain noun or value object. | `CalendarEvent`, `ScheduledStart` |
| Application command | `{Verb}{Domain}Command` | `CreateCalendarEventCommand` |
| Application handler | `{Verb}{Domain}Handler` | `CreateCalendarEventHandler` |
| Application result | `{Verb}{Domain}Result` | `CreateCalendarEventResult` |
| HTTP request | `{Verb}{Domain}Request` | `CreateCalendarEventRequest` |
| HTTP response | `{Verb}{Domain}Response` | `CreateCalendarEventResponse` |
| Persistence entity | `{Domain}Entity` | `CalendarEventEntity` |
| Repository | `{Provider}{Domain}Repository` | `AzureCalendarEventRepository` |
| External adapter or client | `{Provider}{Resource}Adapter`, `{Provider}{Resource}Client`, or a `{Provider}{Role}` port implementation | `YouTubePublisher` |
| Configuration | `{Scenario}Options` | `AuthOptions` |
| Test double | `Fake{Role}` | `FakeCalendarEventModifier` |

When touching current calendar-event code, prefer the fully qualified domain
name when it prevents ambiguity. Prefer shorter names when the namespace,
endpoint, command shape, or containing type supplies the missing context.
For example, in `YTSkedy.Scheduling.Application.Platforms`, prefer
`PublishHandler` over `PublishCalendarEventToPlatformHandler` because the
namespace and command shape already supply the calendar-event and platform
context. Shorter forms such as `event`,
`request`, or `result` are fine for local variables when the enclosing method or
type already provides the missing context.

Avoid stacking every related domain word into a single identifier. Prefer
`PlatformPublication`, `PlatformActionPolicy`, `IPlatformModifier`, and
`AzurePlatformRepository` in platform-focused namespaces over names such as
`CalendarEventPlatformPublication`, `CalendarEventPlatformActionPolicy`,
`ICalendarEventPlatformModifier`, and `AzureCalendarEventPlatformRepository`.

Use provider prefixes such as `Azure` or `YouTube` at infrastructure,
adapter, persistence, and external-client boundaries. Do not put provider
prefixes on domain types unless the domain concept is provider-owned.

## Unit Test Names

Unit test names follow the Microsoft pattern:

```text
MethodName_Scenario_ExpectedBehavior
```

For generic entry points such as `HandleAsync`, `ExecuteAsync`, or `RunAsync`,
the first segment may name the domain behavior or unit of work instead of the
literal method name:

```text
MethodOrBehavior_Scenario_ExpectedBehavior
```

Rules:

- Use `PascalCase` segments separated by underscores.
- Include the method under test, the tested scenario, and the expected behavior.
- Do not prefix test names with `Test`.
- Keep the name readable as executable documentation.
- Use the same method name that production code exposes, including `Async`,
  unless the method is a generic entry point and the behavior name is clearer.
- Prefer a longer clear test name over a short name that hides the requirement,
  but discuss names that exceed the thresholds above.
- Use Arrange, Act, Assert structure inside the test.
- Keep test data explicit, especially dates, time zones, IDs, and language
  codes.

Examples:

```csharp
[Fact]
public async Task CreateCalendarEvent_ValidCommand_CreatesCalendarEvent()
{
}

[Fact]
public void Constructor_ValidInput_SetsProperties()
{
}

[Fact]
public void TryParseTemplateType_KnownType_ReturnsTrue()
{
}

[Fact]
public async Task Publish_FutureEventWithEnglishTitle_PublishesAndMarksPublished()
{
}
```

Name test doubles by their role. Use `Fake` for a hand-written dependency that
can be used as a stub or mock. Use local variable names such as `stubRepository`
or `mockRepository` only when the specific role matters in that test.

## Enforcement

Use `.editorconfig` for objective mechanical C# naming rules such as casing,
interface prefixes, generic type parameter prefixes, events, constants, and
private instance field prefixes. Do not use `.editorconfig` for glossary
choices, word choice, identifier length thresholds, or test-name semantics.
Use `warning` severity for mechanical naming rules unless a rule proves noisy
and needs a narrower scope.

Keep static readonly field naming review-only if `.editorconfig` cannot target
it without also catching excluded private static fields.

Length thresholds, glossary terms, and word choice require code review unless
the project later adds a custom analyzer.

Use `dotnet format` after adding or changing `.editorconfig` naming rules.

## References

Microsoft references:

- [C# identifier naming rules and conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names)
- [.NET general naming conventions](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/general-naming-conventions)
- [.NET names of classes, structs, and interfaces](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-classes-structs-and-interfaces)
- [.NET names of type members](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-type-members)
- [Task-based asynchronous pattern](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap)
- [Unit testing best practices for .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Code-style naming rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/naming-rules)
- [What's new in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- [Options pattern in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)
- [Options pattern guidance for .NET library authors](https://learn.microsoft.com/en-us/dotnet/core/extensions/options-library-authors)
- [Dependency injection guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines)

Secondary influences:

- [Google C# Style Guide](https://google.github.io/styleguide/csharp-style.html)
- [Google Java Style Guide](https://google.github.io/styleguide/javaguide.html)
- [Rust API Guidelines: Naming](https://rust-lang.github.io/api-guidelines/naming.html)
- [Kubernetes coding conventions](https://github.com/kubernetes/community/blob/master/contributors/guide/coding-conventions.md)
- [Ubiquitous Language](https://martinfowler.com/bliki/UbiquitousLanguage.html)
- [Bounded Context](https://martinfowler.com/bliki/BoundedContext.html)
- [Clean code 101: Meaningful names and functions](https://medium.com/coding-skills/clean-code-101-meaningful-names-and-functions-bf450456d90c)
- [Naming standards for unit tests](https://osherove.com/blog/2005/4/3/naming-standards-for-unit-tests.html)
