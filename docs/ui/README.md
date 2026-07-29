# UI Docs

Durable documentation for the Angular frontend under `src/ui/`.

## Ownership

- Source of truth: Angular UI documentation index and UI doc scope.
- Update when: adding, removing, renaming, or moving UI docs, or when UI doc
  ownership changes across architecture, routes, development, testing, styling,
  responsive layout, or runtime configuration.
- Validate with: compare this index against `rg --files docs/ui`, verify edited
  links resolve, and run `git diff --check`.

## Contents

- Architecture: [`architecture.md`](architecture.md)
- Application structure:
  [`architecture/application-structure.md`](architecture/application-structure.md)
- Runtime configuration:
  [`architecture/runtime-configuration.md`](architecture/runtime-configuration.md)
- Routes: [`routes.md`](routes.md)
- Development guidelines:
  [`development/development-guidelines.md`](development/development-guidelines.md)
- Naming conventions:
  [`development/naming-conventions.md`](development/naming-conventions.md)
- Styling: [`development/styling.md`](development/styling.md)
- Icons: [`development/icons.md`](development/icons.md)
- Responsive layout:
  [`development/responsive-layout.md`](development/responsive-layout.md)
- Build and test commands:
  [`development/build-and-test.md`](development/build-and-test.md)
- Testing guidance: [`development/testing.md`](development/testing.md)
- Unit testing: [`development/unit-testing.md`](development/unit-testing.md)
- End-to-end testing:
  [`development/end-to-end-testing.md`](development/end-to-end-testing.md)
- Deployment: [`operations/deployment.md`](operations/deployment.md)

## External References

Use these as the upstream Angular baseline for UI code guidance:

- Official Angular style guide: <https://angular.dev/style-guide>
- Angular.love component practices:
  <https://angular.love/best-practices-of-working-with-angular-components>

## Scope

UI docs own:

- Browser routes, pages, components, forms, and interaction state.
- Frontend API client behavior that consumes backend HTTP contracts.
- Frontend build, test, local development, and deployment guidance.

Backend HTTP contracts, persistence, configuration, and API deployment belong
in [`../api/`](../api/).
