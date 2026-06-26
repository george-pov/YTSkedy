# YTSkedy

YTSkedy is an open source application for production-grade scheduling
automation of YouTube streams.

## Purpose

This README is the human developer entrypoint for YTSkedy. It points to the
main durable documentation entry points. The complete durable docs inventory
lives in [`docs/README.md`](docs/README.md).

## Repository Layout

- `src/api/`: .NET Azure Functions backend API, scheduling domain and
  application projects, infrastructure adapters, backend tests, and manual
  `.http` checks.
- `src/ui/`: Angular frontend workspace, browser application source, frontend
  tests, and npm package metadata.
- `docs/`: durable shared architecture, API, UI, development, and operations
  guidance.
- `.work/`: local-only agent workflow records and transient planning support.

## Documentation

- Project context: [`CONTEXT.md`](CONTEXT.md)
- Durable docs inventory: [`docs/README.md`](docs/README.md)
- Shared architecture overview: [`docs/architecture/overview.md`](docs/architecture/overview.md)
- Cross-boundary integration contracts: [`docs/architecture/integration-contracts.md`](docs/architecture/integration-contracts.md)
- Domain vocabulary and naming guidance: [`docs/development/naming-guidance.md`](docs/development/naming-guidance.md)
- API docs: [`docs/api/README.md`](docs/api/README.md)
- UI docs: [`docs/ui/README.md`](docs/ui/README.md)
