# UI End-to-End Testing

Browser-level tests are optional verification for the Angular app. They
complement unit tests, visual review, accessibility review, and manual checks.
They are not a required release gate, CI gate, or default condition for
merging changes unless the project later adopts that policy.

Playwright is the selected browser-level e2e test runtime for YTSkedy. The
current setup runs durable specs from `src/ui/tests/e2e/` and writes generated
reports under the repository `build/` directory.

Use end-to-end tests only when a real browser adds useful confidence that a
cheaper unit or component test cannot provide.

## When To Add Or Keep A Test

Add or keep a durable browser test when it protects one of these surfaces:

- high-value route smoke coverage
- critical user journeys that cross multiple app-owned boundaries
- browser-only behavior such as routing, focus, navigation, native form
  behavior, or rendering behavior that jsdom cannot represent
- regression coverage for a bug that unit tests cannot catch cheaply

Do not add a browser test just because a component, route, field, or button
exists. A durable test must earn its maintenance cost.

Delete or rewrite a durable browser test when it no longer protects meaningful
browser behavior, fails only because of harmless implementation changes, or
duplicates cheaper unit coverage.

## What To Exclude

Keep these out of durable browser tests:

- static markup checks that do not protect behavior
- UI library internals
- framework-generated classes or DOM structure
- broad visual snapshots
- unstable copy that does not define behavior
- low-value component creation checks
- exact CSS values, coordinates, pixel dimensions, or layout measurements

Copy assertions are appropriate only when the copy identifies a route, labels a
user action, labels a field, or communicates a behavior-defining message.

## Test Layout

Use these locations:

```text
src/ui/tests/e2e/
  Durable Playwright tests.

src/ui/tests/.tmp/
  Temporary local verification specs. This directory is ignored by git.
```

Temporary specs can be useful during development and diagnosis. Before work is
complete, either promote them intentionally into `tests/e2e/` or remove them.

Temporary screenshots are allowed for local verification. Keep generated local
artifacts out of committed source, such as under `build/`. Committed visual
snapshot baselines require explicit approval before they are added.

## Commands

Run these commands from `src/ui/`:

```powershell
npm run start:e2e
npm run test:e2e
npm run test:e2e:all
npm run test:e2e:ui
```

Command expectations:

- `npm run start:e2e` starts the Angular dev server for E2E testing at the
  configured Playwright base URL.
- `npm run test:e2e` runs durable Playwright tests in Chromium by default and
  starts the Angular dev server through the Playwright config.
- `npm run test:e2e:all` runs the explicit all-browser Playwright project
  matrix.
- `npm run test:e2e:ui` opens Playwright UI mode for optional local debugging.

If browser binaries are missing, run:

```powershell
npx playwright install
```

Use `npx playwright install chromium` when only the default Chromium e2e command
needs local browser binaries.

## Authoring Rules

Prefer user-facing locators such as roles, labels, and accessible names. Use
stable app-owned `data-testid` hooks only when user-facing locators cannot
express the interaction clearly or would make the test brittle.

Mock network calls by default when durable browser tests exercise API behavior.
Use live network behavior only when the test explicitly owns that integration
risk.

Use browser-test web assertions instead of fixed sleeps. Wait for visible user
signals such as route changes, controls, headings, or messages.

The contributor changing a related route, component, service boundary, or
browser-test setup owns deciding whether a broken browser test should be fixed,
rewritten, or deleted.

## Decision Checklist

Before adding a durable browser test, answer these questions:

1. What expensive browser-level regression would this catch?
2. Can a unit test cover the same behavior more cheaply?
3. Does the test assert app-owned behavior instead of library internals?
4. Would the test fail during a harmless copy, styling, or markup refactor?
5. Are network calls mocked when API behavior is involved?
6. Is Chromium coverage enough, or is there a clear reason to run every
   configured browser?
7. Is the test small enough that future contributors will trust it?
8. If the test fails later, is the fix, rewrite, or delete decision clear?

If the answers do not identify a stable browser-level contract, keep the check
manual, temporary, or covered by unit tests.

## Failure Modes And Recovery

Common browser-test failures and recovery paths:

- Missing browser binaries: run the framework's browser install command, then
  rerun the targeted command.
- Local server failure: confirm the E2E server command, base URL, and port are
  aligned between `package.json` and the test config.
- Port conflict: stop the conflicting local server or use the configured web
  server reuse behavior.
- Brittle locator: replace framework selectors or markup-depth selectors with
  roles, labels, accessible names, or stable app-owned `data-testid` hooks.
- Unstable copy assertion: assert only route identity, labels, actions, or
  behavior-defining messages.
- Network nondeterminism: mock the API boundary for durable tests that
  exercise API behavior.
- Timing flake: wait for a user-visible state change with assertions instead
  of adding sleeps.
- Low-value failure: delete or rewrite the test when it no longer earns its
  maintenance cost.

The default recovery path is to reduce the test to the smallest stable
browser-level behavior it protects. If no such behavior remains, delete it.
