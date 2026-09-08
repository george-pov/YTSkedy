# Project Context

Durable project context for YTSkedy. This file owns product posture, supported
scope, system boundaries, production constraints, and security expectations.
Detailed implementation and contract documentation lives under `docs/`.

## Ownership

- Source of truth: stable product context and cross-cutting production
  constraints.
- Update when: product scope, supported providers, system boundaries, production
  posture, or cross-cutting security expectations change.
- Do not duplicate: endpoint shapes, configuration keys, persistence schemas,
  page behavior, or identifier guidance owned by linked documents.

## Product Goal

YTSkedy is an open source application that automates the repetitive work needed
to create, schedule, and publish live-stream content. It is designed for
creators and channel operators who need repeatable scheduling, consistent
metadata, explicit external side effects, and safe time-zone handling.

The project is maintained as a production-grade application. Documentation,
configuration, validation, telemetry, recovery, and security decisions must be
suitable for external contributors and real deployments.

## Current Product Scope

- An Angular browser UI supports authenticated scheduling and management flows.
- A .NET Azure Functions API owns validation, persistence, scheduling rules,
  authorization, and external integration orchestration.
- Calendar events store explicit local scheduled time plus time-zone context and
  expose provider-neutral application state.
- Reusable templates and configurable event text fields produce publishing
  content from stored calendar-event values.
- Configured platforms represent publishing destinations and keep provider
  settings separate from calendar events.
- YouTube and WordPress are supported publishing providers behind the platform
  abstraction.
- YouTube platforms may define a video category and altered or synthetic
  content disclosure as defaults for future publications. The browser uses a
  reviewed static US category catalog; the backend does not provide runtime
  YouTube category discovery.
- Per-platform publication records track provider work and external resource
  identifiers independently from the calendar event. Caught publish failures
  are operator-visible, retryable records that may retain an external resource
  id and a secret-safe failure summary for provider verification and log
  correlation.
- Reads and publish preflight remain request-cancelable. After a publication
  attempt starts, a server-owned deadline bounds provider work and a separate
  short deadline bounds final-state persistence. Known provider ids are
  checkpointed before later provider work.
- Hard termination can still leave a `Publishing` row. The backend exposes
  recovery eligibility only for an active future attempt older than the
  configured stale threshold, and an authenticated operator can conditionally
  mark that exact row `Failed` after verifying the provider.
- Calendar-event thumbnails are application-owned artifacts and may be applied
  to supported provider resources during publication.

Exact HTTP behavior is documented in [`docs/api/http/`](docs/api/http/). Browser
behavior is documented in [`docs/ui/routes.md`](docs/ui/routes.md).

## System Boundaries

- The Angular UI owns presentation, browser interaction state, route behavior,
  and user input collection.
- The backend API owns durable rules, server-side validation, authorization,
  persistence, provider orchestration, and externally visible side effects.
- Application and domain code own provider-neutral scheduling and publishing
  behavior.
- Infrastructure adapters own Azure Storage, YouTube, WordPress, authentication
  framework, and other external-service integration details.
- External providers own their resources, enforcement rules, availability,
  quotas, and provider-side processing.
- The UI consumes backend-computed action eligibility and must not duplicate
  scheduling or publication policy.

The shared system map and dependency direction are documented in
[`docs/architecture/overview.md`](docs/architecture/overview.md). Cross-boundary
ownership rules live in
[`docs/architecture/integration-contracts.md`](docs/architecture/integration-contracts.md).

## Production Constraints

- Scheduling uses explicit instants and time zones. Browser or server local time
  is not an implicit source of truth.
- External writes must expose clear retry, partial-failure, reconciliation, and
  recovery behavior before the affected capability is considered production
  complete.
- Provider cleanup and application persistence must preserve enough state to
  diagnose and recover from partial failures.
- Retrying a failed publication is an explicit operator action after checking
  the publishing platform and removing any uncertain provider resource when
  necessary.
- Provider writes are not automatically retried, and uncertain provider
  resources are not automatically deleted.
- Public HTTP contracts must remain stable enough for independent UI and API
  work.
- Deployment-specific values belong in environment configuration rather than
  source code.
- Hosted application storage uses the Function App system-assigned identity and
  data-plane roles. Hosted tables and containers are provisioned by Bicep;
  request handling never creates storage resources.
- Backup, migration, recovery, telemetry, rate-limit handling, and credential
  lifecycle behavior must be documented before relying on them operationally.

API configuration and persistence details live in
[`docs/api/configuration.md`](docs/api/configuration.md) and
[`docs/api/persistence.md`](docs/api/persistence.md). Operational guidance lives
under `docs/api/operations/` and `docs/ui/operations/`.

## Security And Privacy

- Never commit OAuth client secrets, access tokens, refresh tokens, API keys,
  passwords, storage credentials, function keys, private certificates, or local
  credential stores.
- Treat provider credentials and tokens as secret-bearing at every boundary.
- Redact secret material from HTTP reads, logs, snapshots, screenshots, test
  fixtures, and validation records.
- Browser runtime configuration contains public client settings only.
- Authentication establishes identity. Authorization still controls every
  protected API operation.
- Use least-privilege scopes, roles, identities, and deployment permissions.
- Do not expose provider or storage implementation details through the UI unless
  they are part of an intentional public contract.

## Durable Documentation Map

- Complete inventory: [`docs/README.md`](docs/README.md)
- System architecture: [`docs/architecture/overview.md`](docs/architecture/overview.md)
- Integration ownership: [`docs/architecture/integration-contracts.md`](docs/architecture/integration-contracts.md)
- API documentation: [`docs/api/README.md`](docs/api/README.md)
- UI documentation: [`docs/ui/README.md`](docs/ui/README.md)
- Domain vocabulary: [`docs/development/domain-vocabulary.md`](docs/development/domain-vocabulary.md)
- Naming rules: [`docs/development/naming-guidance.md`](docs/development/naming-guidance.md)
