# UI Testing

Frontend tests live under `src/ui/`. Use this file as the testing index.

## Canonical Guidance

- Unit testing policy: [`unit-testing.md`](unit-testing.md)
- End-to-end testing policy: [`end-to-end-testing.md`](end-to-end-testing.md)
- Runnable commands: [`build-and-test.md`](build-and-test.md)

## Current State

- The Angular test setup is exposed through `npm test`.
- Vitest with jsdom is the current unit test runtime.
- Current route tests live in `src/ui/src/app/app.routes.spec.ts`.
- Playwright is the current browser-level e2e test runtime.
- The first durable e2e spec lives in
  `src/ui/tests/e2e/component-lab-button.spec.ts`.

## Boundary Rules

- Mock or fake backend HTTP calls in unit tests.
- Do not require a live Azure Functions host, Azure Storage, YouTube,
  WordPress, or real credentials for frontend unit tests.
- Keep date, time zone, locale, and API response fixture values explicit.
- Do not duplicate backend scheduling rules in UI tests.
