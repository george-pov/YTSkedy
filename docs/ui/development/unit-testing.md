# UI Unit Testing

Unit tests in the YTSkedy Angular app protect behavior, app-owned contracts,
and risky transformations. They are not a substitute for visual review,
accessibility audits, browser testing, or end-to-end workflow tests.

The goal is a small suite that developers trust. A test should make
refactoring safer without forcing updates after harmless template, styling, or
UI library changes.

## Test Runtime

The app uses the Angular CLI unit test builder with Vitest and jsdom.

Run unit tests from `src/ui/`:

```powershell
npm test
```

Run the build before considering larger UI changes complete:

```powershell
npm run build
```

Tests live beside the code under test:

```text
calendar-events-api.ts
calendar-events-api.spec.ts

calendar-events-store.ts
calendar-events-store.spec.ts

calendar-events.ts
calendar-events.spec.ts
```

A file does not need a `.spec.ts` only because it exists. Add tests for
meaningful behavior, not for file count or coverage padding.

## What To Test

Always test:

- pure transformations and mapping functions
- validators and branching rules
- API request shape and response mapping
- invalid action gating
- app-owned shared form-control logic
- UI adapter import boundaries for pages and layout
- bug fixes, unless the bug is visual, browser-specific, environmental, or
  better verified at another layer

Usually test:

- component orchestration that coordinates form validity, service calls,
  loading state, success state, or error state
- signal state transitions when they drive visible behavior or a public output
- runtime configuration parsing with light, focused coverage
- route-table behavior with light, focused coverage
- API URL and request-shape contracts with light, focused coverage

Rarely test in unit specs:

- exact markup depth or element order
- static copy just because it exists in a template
- CSS classes used only for styling
- full rendered pages
- component creation smoke tests with no behavior assertion

Escalate outside unit tests:

- visual fidelity
- responsive layout
- cross-browser behavior
- complete user journeys
- accessibility audits that require browser or accessibility tooling

## UI Library Boundary

Pages and page-local components should use app-owned shared UI components from
`src/ui/src/app/shared/components/` when a wrapper exists. They should not
import a UI library directly for covered controls.

Shared UI adapters may import UI libraries internally. Their public inputs and
outputs must describe app intent, not the underlying library implementation.

Unit tests must not assert UI library internals:

- no library component instance assertions
- no library-generated DOM assertions
- no visual mapping assertions such as appearance or internal classes

Test a shared UI adapter only when it contains meaningful app-owned logic, such
as value propagation, multi-select add/remove rules, date conversion, or native
behavior that affects form submission.

## Component Tests

Prefer user-visible behavior and public component contracts. Use DOM
interaction for simple user-triggered behavior, such as selecting a month and
clicking a button.

Direct component method calls are acceptable when they keep the test focused
and the method is a deliberate component boundary. Do not reach into private or
protected state with `as any`.

Good component tests answer questions such as:

- Does the page wait for both year and month before loading events?
- Does selecting a valid year and month call the service with expected values?
- Does an empty response show the empty state?
- Does a failed request show the error state?
- Does a public output emit when the user takes the action?

Avoid tests that only confirm a page renders static text. Route identity,
request shape, and user-facing state transitions can be worth testing because
they are behavior contracts.

## Forms And Validation

Keep nontrivial validation and branching rules outside large page components.
Use named validator functions, form-state builders, pure functions, or
page-owned services. Test those directly.

Component tests should verify form integration only when the integration is
risky or user-visible.

Do not duplicate backend scheduling rules in UI tests. Keep browser validation
focused on user interaction and request readiness.

## HTTP Services

HTTP service tests should assert the app-owned request contract: method, URL,
query parameters, payload, and relevant response or error behavior. Use
Angular HTTP testing utilities when `HttpClient` is introduced.

Do not mock the service under test. Mock or fake external, slow, or
nondeterministic boundaries.

For calendar events, protect these contracts:

- `GET /api/calendar-events?year={year}&month={month}` query shape
- local date-time and time-zone values are not converted to browser local time
- English and Russian titles are mapped by language code, not array order
- missing localized titles map to blank display values

## Routes And Runtime Configuration

Routes, guards, and runtime config deserve light coverage because failures are
visible and usually cheap to diagnose. Keep these tests narrow.

Good examples:

- root route resolves to the supported first page
- wildcard route behavior matches the documented route table
- config parser rejects missing required settings
- config loader trims and stores the loaded setting

Avoid broad navigation simulations when a small route-table or parser test is
enough.

## Tests To Avoid

Do not keep component creation smoke tests with no behavior assertion:

```typescript
it('should create', () => {
  expect(component).toBeTruthy();
});
```

Do not assert private or protected component state:

```typescript
expect((component as any).loading()).toBe(false);
```

Do not use snapshots for Angular component output. They tend to protect markup
noise instead of app behavior.

Do not write CSS layout assertions in unit tests. Use visual review, browser
tests, or visual regression tooling when layout fidelity needs automated
coverage.

## Review Questions

Before adding or keeping a unit test, ask:

1. What expensive regression would this catch?
2. Is the assertion on an app-owned contract?
3. Would this fail during a harmless refactor?
4. Can the logic move to a pure function, validator, service, or state object?
5. Is a mock hiding the behavior the test claims to protect?
6. Is jsdom enough, or is this really browser or end-to-end coverage?
7. Does the assertion inspect generated DOM, visual mapping, or private
   component state?

If a test does not protect a meaningful app behavior or contract, delete it.
