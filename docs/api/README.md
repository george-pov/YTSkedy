# API Docs

Durable documentation for the backend API under `src/api/`.

## Ownership

- Source of truth: backend API documentation index and API doc scope.
- Update when: adding, removing, renaming, or moving API docs, or when API doc
  ownership changes across architecture, HTTP contracts, persistence,
  configuration, development, testing, or operations.
- Validate with: compare this index against `rg --files docs/api`, verify
  edited links resolve, and run `git diff --check`.

## Contents

- Architecture: [`architecture.md`](architecture.md)
- HTTP contracts: [`http/`](http/)
- Calendar events HTTP contract:
  [`http/calendar-events.md`](http/calendar-events.md)
- Platform publishing HTTP contract:
  [`http/platforms.md`](http/platforms.md)
- Persistence: [`persistence.md`](persistence.md)
- Configuration: [`configuration.md`](configuration.md)
- Build and test commands:
  [`development/build-and-test.md`](development/build-and-test.md)
- Testing guidance: [`development/testing.md`](development/testing.md)
- Deployment: [`operations/deployment.md`](operations/deployment.md)
- YouTube publish setup:
  [`operations/youtube-publish-setup.md`](operations/youtube-publish-setup.md)
- Platform publication cleanup:
  [`operations/platform-publication-cleanup.md`](operations/platform-publication-cleanup.md)

## Scope

API docs own:

- Azure Functions host behavior.
- HTTP request, response, route, authorization, and error contracts.
- Scheduling application and domain placement rules.
- Infrastructure adapters, persistence, configuration, and external
  integration boundaries.
- Backend build, test, manual HTTP check, and deployment guidance.

UI route, component, browser state, and frontend build guidance belong in
[`../ui/`](../ui/).
