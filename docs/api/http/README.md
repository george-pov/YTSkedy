# API HTTP Contracts

Durable HTTP contract documentation for the backend API. These files are the
canonical owners for endpoint routes, request and response shapes, status
codes, authorization requirements, and manual API check notes.

## Ownership

- Source of truth: backend HTTP endpoint contracts consumed by the UI and
  manual API clients.
- Update when: route, method, request shape, response shape, status code,
  authorization requirement, error behavior, or manual API check guidance
  changes.
- Validate with: check affected endpoint tests or manual checks when runtime
  behavior changes, verify cross-boundary docs link here instead of duplicating
  endpoint details, and run `git diff --check`.

## Contracts

- Calendar events: [`calendar-events.md`](calendar-events.md)
- Event text fields: [`event-text-fields.md`](event-text-fields.md)
- Calendar-event thumbnails:
  [`calendar-event-thumbnails.md`](calendar-event-thumbnails.md)
- Configured platforms: [`platforms.md`](platforms.md)
- Platform publications: [`platform-publications.md`](platform-publications.md)
- Templates: [`templates.md`](templates.md)

Keep request and response shapes here when they are externally consumed by the
UI or manual API clients. The cross-boundary producer and consumer map lives in
[`../../architecture/integration-contracts.md`](../../architecture/integration-contracts.md).
Boundary-specific implementation notes belong in
[`../architecture.md`](../architecture.md), [`../persistence.md`](../persistence.md),
or [`../configuration.md`](../configuration.md).
