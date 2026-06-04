# UI Naming Conventions

Naming conventions for the YTSkedy Angular frontend.

Shared domain vocabulary lives in
[`../../development/naming-guidance.md`](../../development/naming-guidance.md).
Use that vocabulary for domain terms such as `CalendarEvent`,
`ScheduledStream`, `StreamTemplate`, `ScheduledStart`, and `ChannelOperator`.

## Upstream Angular Reference

Use the [official Angular style guide](https://angular.dev/style-guide) as the
upstream baseline for Angular naming and structure. This document narrows that
baseline for the YTSkedy frontend where local conventions are more specific.

Also review
[Angular.love's component practices](https://angular.love/best-practices-of-working-with-angular-components)
when naming or reorganizing components. That article recommends explicit
`.component` and `.service` suffixes for discoverability. YTSkedy deliberately
does not adopt that suffix rule for new code because the official Angular
style guide and the current Angular CLI defaults use concise file and class
names. Existing suffixed names do not need churn-only cleanup.

## General Rules

- Use kebab-case for folders and file names.
- Match file names to the primary TypeScript identifier in the file.
- Use concise Angular file names for new app code.
- Do not add dot-separated artifact suffixes such as `.component`, `.service`,
  `.model`, `.interface`, or `.type` by default.
- Keep one primary concept per file.
- Keep tests beside the code under test with `.spec.ts`.
- Avoid generic file names such as `utils.ts`, `helpers.ts`, or `common.ts`.

Examples:

```text
calendar-events.ts
calendar-events.html
calendar-events.scss
calendar-events.spec.ts

calendar-events-api.ts
calendar-events-api.spec.ts

calendar-event-row.ts
scheduled-start-option.ts
```

## Folders

Folder names should describe the route segment, page flow, or component they
own. Use kebab-case and avoid type-only folders inside page flows until the
file count justifies the grouping.

Current examples:

```text
pages/calendar-events/
```

Target examples as the app grows:

```text
layout/app-layout/
pages/calendar-events/
shared/components/month-select/
shared/components/year-select/
```

## Components

For new components, use Angular's concise component naming style:

```text
[name].ts
[name].html
[name].scss
[name].spec.ts
```

The folder name, component file name, class name, and selector should describe
the same concept:

```text
shared/components/month-select/month-select.ts
MonthSelect
app-month-select
```

Use `app-` as the component selector prefix. Keep selectors kebab-case.

Do not use the older `.component` file suffix or `Component` class suffix for
new Angular component files and classes.

## Pages And Routes

Route-owned folders should mirror the route path when practical:

```text
calendar-events
```

Do not add `Page` to a route component name by default. Use the domain name
already carried by the route, such as `CalendarEvents`. Add a suffix only when
it clarifies a real distinction between a routed container and a local child
component.

## Shared UI Components

Shared UI names should describe the app contract, not an underlying UI library.

Use these suffixes consistently:

- `*-field` for input-like form controls.
- `*-select` for select controls.
- `*-group` for grouped choices.
- no provider-specific terms in public wrapper names.

Selectors should match the component name with the `app-` prefix:

```text
app-month-select
app-year-select
app-calendar-events-table
```

## Services, Models, And Types

For new injectable classes, app contracts, interfaces, type aliases, and value
models, follow Angular's concise naming style. Pick a primary identifier that
describes the role or data shape, then use its kebab-case file name:

```text
CalendarEventsApi
calendar-events-api.ts
calendar-events-api.spec.ts

CalendarEventsStore
calendar-events-store.ts

CalendarEventRow
calendar-event-row.ts

ScheduledStartOption
scheduled-start-option.ts
```

Do not use dot-separated artifact suffixes for new files:

```text
calendar-events.service.ts
calendar-event-row.model.ts
scheduled-start-option.type.ts
```

A class may include `Service` when that is the clearest domain name or when it
avoids ambiguity, but the file still mirrors the identifier with hyphens:

```text
CalendarEventsService
calendar-events-service.ts
```

Feature-owned services should stay near the page flow that owns them, such as
`pages/calendar-events/services/calendar-events-api.ts`. Move services, models,
or shared contracts to `shared/` only when they are reused across page flows.

## Utilities

A `shared/utils/` folder may be introduced for small shared pure functions, but
file names inside it must describe behavior:

```text
format-scheduled-start.ts
extract-localized-title.ts
build-calendar-events-query.ts
```

Do not create generic utility barrels or catch-all files:

```text
utils.ts
helpers.ts
common.ts
```

## Class Members

For new Angular code:

- prefer `inject()` for dependencies
- mark injected dependencies, inputs, outputs, signals, and stable config as
  `readonly`
- use `protected` for members used only by the template
- name event handlers for the action they perform when the action is specific

Existing code does not need churn-only renaming. Improve names when touching
the same area for a feature or bug fix.

## Styles

Use [`styling.md`](styling.md) for CSS class naming and styling rules.
