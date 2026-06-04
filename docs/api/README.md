# API Docs

Durable documentation for the backend API under `src/api/`.

## Contents

- Architecture: [`architecture.md`](architecture.md)
- HTTP contracts: [`http/`](http/)
- Calendar events HTTP contract:
  [`http/calendar-events.md`](http/calendar-events.md)
- Persistence: [`persistence.md`](persistence.md)
- Configuration: [`configuration.md`](configuration.md)
- Build and test commands:
  [`development/build-and-test.md`](development/build-and-test.md)
- Testing guidance: [`development/testing.md`](development/testing.md)
- Deployment: [`operations/deployment.md`](operations/deployment.md)

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
