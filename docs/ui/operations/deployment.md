# UI Deployment

The Angular frontend under `src/ui/` requires a production deployment target
and workflow before the application can ship a full browser experience.

## Current Release Status

The current GitHub Actions deployment workflow deploys only the backend Azure
Functions app. It does not install npm packages, build the Angular app, publish
frontend assets, or deploy a frontend host.

## Required Deployment Decisions

Before a production frontend release, document:

- The hosting platform and artifact path.
- The build command and Node or npm version policy.
- How the browser app receives the API base URL through runtime
  configuration.
- Whether frontend deployment is independent or coordinated with API
  deployment.
- Required environment variables, secrets, and GitHub Environment settings.

Do not commit API keys, OAuth secrets, function keys, deployed host values that
are user-specific, or local credential stores.

Use the build-once, configure-per-deployment approach in
[`../architecture/runtime-configuration.md`](../architecture/runtime-configuration.md)
when the first deploy-specific UI setting is introduced.
