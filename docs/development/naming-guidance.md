# Naming Guidance

This document defines identifier naming rules for YTSkedy code and tests.
Canonical product terms live in [`domain-vocabulary.md`](domain-vocabulary.md).

## Ownership

- Source of truth: identifier naming rules, operation verbs, layer conventions,
  test naming, and mechanical naming enforcement.
- Update when: changing preferred identifier forms, naming thresholds,
  operation verbs, layer conventions, or mechanical naming enforcement.
- Validate with: compare touched identifiers against this guidance, verify
  references when adding external guidance, run `git diff --check`, and run
  `dotnet format` only when `.editorconfig` naming rules change.

## Scope

The domain vocabulary linked above applies across backend API code, frontend UI
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
- Name collection properties with plural nouns such as `Texts`, not
  `TextList`.
- Async methods that return `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>`
  must end with `Async`.
- Methods that accept cancellation should name the parameter
  `cancellationToken`.
- Options classes should end in `Options`, such as `AuthOptions`.
- Service registration extension methods should use `Add{Service}` for
  reusable registration methods.

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

Canonical product terms live in
[`domain-vocabulary.md`](domain-vocabulary.md#domain-vocabulary).

## Code Identifier Glossary

Preferred public identifier vocabulary lives in
[`domain-vocabulary.md`](domain-vocabulary.md#code-identifier-glossary).

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

Use boundary-message terms consistently:

- Use `Request` for strongly typed data entering an HTTP endpoint, application
  port, provider port, or external client. This includes nested request-only
  JSON objects.
- Use `Response` for strongly typed data returned across a boundary.
- Use an unqualified domain noun or value-object name for a shape intentionally
  shared by requests and responses.
- Reserve `Input` for UI controls, form values, or plain-language descriptions
  of entered data. Do not use `Input` as a transport DTO suffix.
- Reserve `Payload` for opaque or raw content such as untyped JSON, text,
  bytes, or a signed provider body. Do not use `Payload` for a strongly typed
  request object.
- Keep `Command` and `Result` for application use-case intent and outcomes.

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
public async Task Publish_FutureEventWithRequiredTextValues_PublishesAndMarksPublished()
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

Length thresholds, glossary terms, and word choice require code review unless a
custom analyzer enforces them.

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
