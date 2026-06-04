# Naming Guidance

This document defines domain vocabulary and naming rules for YTSkedy code and
tests.

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
| Unit test method | 80 chars | 95 chars | 120 chars |

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
  `IsEnabled`, `CanSchedule`, or `HasThumbnail`.
- Name collection properties with plural nouns such as `Descriptions`, not
  `DescriptionList`.
- Async methods that return `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>`
  must end with `Async`.
- Methods that accept cancellation should name the parameter
  `cancellationToken`.
- Options classes should end in `Options`, such as `AzureStorageOptions`.
- Service registration extension methods should use `Add{Service}` when this
  project later exposes reusable registration methods.

## Meaningful Names

Names should reveal intent without a comment. If the reader needs a comment to
know why the symbol exists, improve the name or the model.

- Use one word per concept. Do not mix `Fetch`, `Retrieve`, and `Get` for the
  same operation.
- Keep names pronounceable and searchable.
- Avoid names that differ only by small qualifier changes.
- Avoid generic class words such as `Manager`, `Processor`, `Data`, `Info`,
  `Helper`, and `Util` unless a clearer domain or role name is not available.
- Prefer a named value object, command, request, or options type when a method
  needs more than three independent values.
- Use static factory methods such as `FromLocalTime` or `FromYouTubeResource`
  when overloaded constructors would hide what the arguments mean.
- Keep command methods and query methods separate. A method named `Get` should
  not create, update, delete, bind, or schedule external resources.

## Operation Verbs

Use these verbs consistently.

| Verb | Meaning |
| --- | --- |
| `Get` | Return an existing value or cheap local result. No external write. |
| `Find` | Search for an optional local or persisted value. Nullable or option-like result is expected. |
| `List` | Return multiple existing items. |
| `Create` | Create a local or external resource. |
| `Schedule` | Turn user intent into a future scheduled stream or broadcast. |
| `Bind` | Link a YouTube broadcast to a YouTube live stream. Use mainly at the YouTube boundary. |
| `Validate` | Check rules and report failures. No mutation. |
| `Map` | Convert between API, application, domain, or persistence models. |
| `Load` | Read from persistence, filesystem, or configuration. |
| `Save` | Persist an application-owned record. |
| `Delete` | Remove a resource when the external API or persistence API uses delete semantics. |
| `Handle` | Execute an application command through a handler. |

Use YouTube API verbs such as `Insert`, `Update`, `Delete`, `Bind`, and
`Transition` only in YouTube adapter code where matching the external contract
helps the reader. Application code should prefer product verbs.

Name externally visible writes with the resource being changed. For example,
prefer `CreateBroadcastAsync`, `BindYouTubeLiveStreamAsync`, or
`SaveCalendarEventAsync` over vague names such as `ProcessAsync` or
`SubmitAsync`.

## Domain Vocabulary

- `scheduled stream`: A future YouTube live event prepared before its start
  time.
- `broadcast`: The public YouTube live event metadata, including title,
  description, visibility, and scheduled start time.
- `stream setup`: The ingestion-side setup that a broadcast uses for encoder
  connection details.
- `stream template`: Reusable local settings for repeated scheduled streams.
- `scheduling plan`: An in-progress, user-reviewable set of one or more planned
  stream events derived from calendar input before any YouTube broadcasts or
  stream setup resources are created.
- `scheduled start time`: The intended start instant for a broadcast, stored
  with enough time zone context to avoid ambiguity.
- `channel operator`: The user who authorizes YTSkedy to create or manage
  scheduled streams for a YouTube channel.
- `credential store`: Local or external storage for OAuth tokens and related
  authentication material.

Verify exact YouTube resource names, required fields, and API behavior against
official Google or YouTube documentation before implementation.

## Code Identifier Glossary

Use these terms as defaults in namespaces, public types, cross-layer contracts,
tests, and documentation. This table is not a ban list. Shorter names are
acceptable in narrow local scopes when the containing namespace, type, method,
or test name already makes the concept obvious.

Prefer full domain terms for:

- public application, API, persistence, and integration-boundary types
- externally visible request, response, command, result, and entity names
- names where both local application state and YouTube-owned state are nearby
- credential, token, time-zone, and scheduled-time concepts

Shorter names are acceptable for:

- local variables and parameters inside a clearly named method
- private helper methods inside a clearly named type
- test variables where the test name already supplies the domain context
- DTO properties when the surrounding DTO name already supplies the context

| Preferred term | Default use | Shorter or alternate form |
| --- | --- | --- |
| `CalendarEvent` | Application-owned calendar input or persisted scheduling row. | `Event` is acceptable inside calendar-event-specific code when it cannot be confused with a YouTube broadcast or .NET event. |
| `ScheduledStream` | Future stream planned by YTSkedy. | `StreamEvent` is usually less clear. Use only in a narrow context where the planned-stream meaning is explicit. |
| `SchedulingPlan` | User-reviewable plan before external resources are created. | `Plan` is acceptable inside scheduling-plan-specific types or tests. Avoid `PlanData` unless the value is only a DTO or serialized shape. |
| `ScheduledStart` | Local date/time plus explicit time zone context. | `StartDate` or `StartTime` is acceptable only when the value is truly date-only or time-only, or when the enclosing type already owns the scheduling context. |
| `ScheduledStartUtc` | Persisted UTC instant derived from the scheduled start. | `UtcStart` is acceptable in storage or formatting helpers. Avoid bare `Date` unless the local scope is very small and unambiguous. |
| `Broadcast` | YouTube live event metadata concept. | `LiveEvent` is acceptable only for user-facing wording or external terminology that requires it. |
| `YouTubeBroadcast` | YouTube `liveBroadcast` resource or adapter DTO. | `Broadcast` is acceptable inside YouTube-specific adapters when the provider context is already clear. |
| `StreamSetup` | Encoder or ingestion setup used by a broadcast. | `Setup` is acceptable only inside a stream-setup-specific type or private helper. |
| `YouTubeLiveStream` | YouTube `liveStream` resource or adapter DTO. | `Stream` is acceptable inside YouTube live-stream adapter code. Avoid it where scheduled streams or stream setup are also in scope. |
| `StreamTemplate` | Reusable title, description, thumbnail, start-time, and metadata defaults. | `Template` is acceptable inside stream-template-specific code. Avoid `TemplateData` unless the value is only a DTO or serialized shape. |
| `LocalizedStreamText` | Title and description for one language or locale. | `LocalizedText` is acceptable when the surrounding type already states that the text belongs to a stream. |
| `Thumbnail` | User-owned image associated with a stream or broadcast. | `Image` is acceptable only when the value is not specifically a thumbnail. |
| `Visibility` | YouTube visibility or privacy state. | `Privacy` is acceptable when mirroring YouTube field names or user-facing wording. |
| `ChannelOperator` | User authorizing work for a YouTube channel. | `User` is acceptable only in authentication or UI-adjacent code where the role distinction does not matter. |
| `CredentialStore` | Storage for OAuth tokens or credential material. | `TokenStore` or `TokenStorage` is acceptable only when the store truly contains tokens and not broader credential material. |
| `CredentialReference` | Non-secret pointer to stored credential material. | `Credential` is acceptable only when the value actually contains credential material or the local context makes the reference nature obvious. |
| `DryRun` | Execution mode that makes no external writes. | `TestMode` should be reserved for test infrastructure, not production dry-run behavior. |
| `Preview` | User-visible planned result before creation. | `Simulation` is acceptable only when the behavior models outcomes, not just displays a plan. |
| `AuditLog` | Durable record of externally visible or security-sensitive actions. | `Log` is acceptable inside logging infrastructure, but use `AuditLog` for durable audit records. |

Use `Id` suffixes for identifiers: `CalendarEventId`, `BroadcastId`,
`StreamId`, `ChannelId`, and `CredentialId`.

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
| External client | `{Provider}{Resource}Client` | `YouTubeBroadcastClient` |
| Configuration | `{Scenario}Options` | `AzureStorageOptions` |
| Test double | `Fake{Role}` | `FakeCalendarEventRepository` |

When touching current calendar-event code, prefer the fully qualified domain
name for public application and API types. For example, prefer
`CreateCalendarEventCommand` over `CreateEventCommand`. Shorter forms such as
`event`, `request`, or `result` are fine for local variables when the enclosing
method or type already provides the missing context.

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
public async Task CreateAsync_DuplicateScheduledStart_ThrowsInvalidOperationException()
{
}

[Fact]
public async Task CreateCalendarEventAsync_MissingBody_ReturnsBadRequest()
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

- [Clean code 101: Meaningful names and functions](https://medium.com/coding-skills/clean-code-101-meaningful-names-and-functions-bf450456d90c)
- [Naming standards for unit tests](https://osherove.com/blog/2005/4/3/naming-standards-for-unit-tests.html)
