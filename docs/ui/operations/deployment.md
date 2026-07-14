# UI Deployment

Portable deployment guidance for the YTSkedy Angular application. The current
deployment target is Azure Storage static website hosting, driven by GitHub
Actions.

## Ownership

- Source of truth: portable UI hosting, deployment workflow, runtime
  configuration injection, deployment permissions, cache behavior, validation,
  and rollback rules.
- Update when: the hosting target, workflow contract, required environment
  variables, artifact layout, permission model, or deployment verification
  changes.
- Excludes: concrete resource names, deployed URLs, tenant values, subscription
  values, and repository-specific federation subjects.

## Hosting Model

The Angular production build is deployed to an Azure Storage static website.
The host is a public static origin only. Browser authentication remains in the
SPA and authorization remains enforced by the Azure Functions API.

The static website configuration uses:

- `$web` as the deployment container.
- `index.html` as the index document.
- `index.html` as the error document so client-side routes return the SPA shell.

Custom domains, edge routing, and CDN behavior require separate durable
documentation when introduced.

## Workflow

`.github/workflows/deploy-azure-ui.yml` builds, tests, packages, configures, and
deploys the UI. It runs for pushes to `main` and supports an explicit GitHub
Environment through manual dispatch.

Pushes and the manual default target `dev`. Prod requires manual selection of
the protected `prod` GitHub Environment and its required approval. A push to
`main` does not deploy to prod.

The workflow:

1. Checks out the repository.
2. Installs the Node and npm versions declared by the workflow.
3. Runs `npm ci`, `npm run build`, and `npm run test:coverage` in `src/ui`.
4. Uploads the browser build as an artifact.
5. Downloads the artifact in the deployment job.
6. Writes the environment-provided runtime configuration.
7. Rejects secret-like runtime configuration content.
8. Replaces the static website content with cache-aware upload ordering.

The workflow owns the exact build output, deploy package, and artifact paths.
Those values must remain aligned with the Angular build configuration.

## GitHub Environment Contract

GitHub Environments `dev` and `prod` each supply these variables with values for
only that environment:

| Name | Classification | Purpose |
| --- | --- | --- |
| `AZURE_CLIENT_ID` | Non-secret | Client id for the Azure deployment identity. |
| `AZURE_TENANT_ID` | Non-secret | Azure tenant used for OIDC login. |
| `AZURE_SUBSCRIPTION_ID` | Non-secret | Azure subscription containing the target storage account. |
| `AZURE_UI_RESOURCE_GROUP` | Non-secret | Resource group containing the UI storage account. |
| `AZURE_UI_STORAGE_ACCOUNT_NAME` | Non-secret | Target static website storage account. |
| `AZURE_UI_STATIC_WEBSITE_URL` | Non-secret | Primary static website origin used for verification. |
| `UI_APP_CONFIG_JSON` | Public runtime configuration | Complete deployed `app-config.json` content. |

`UI_APP_CONFIG_JSON` is delivered to every browser. It may contain public API
URLs, Entra authority values, SPA client ids, redirect URIs, and OAuth scopes.
It must not contain client secrets, access tokens, refresh tokens, function
keys, storage credentials, SAS tokens, passwords, or private certificates.

The runtime file shape is owned by
[`../architecture/runtime-configuration.md`](../architecture/runtime-configuration.md).

Concrete resource names, URLs, identity values, and runtime JSON belong in the
selected GitHub Environment and local operations records. Do not copy dev
values into prod.

## Azure Identity And RBAC

Deployment uses GitHub OIDC with an Azure managed identity or service principal.
Routine deployment must not use storage account keys, connection strings,
publish profiles, or function keys.

Each environment has a separate deployment identity. It requires Storage Blob
Data Contributor only at the matching UI storage account scope and a federated
credential with this subject shape:

```text
repo:OWNER/REPOSITORY:environment:<environment>
```

The dev and prod subjects, identities, and role scopes must not be shared.
Concrete values belong in local operational records, not durable docs.

## Runtime Configuration

The workflow writes `UI_APP_CONFIG_JSON` to
`config/app-config.json` inside the downloaded build artifact and removes the
sample config before upload. It validates both JSON syntax and forbidden
secret-like patterns.

The deployment follows build-once, configure-per-environment behavior. Rebuild
is not required when only public runtime configuration changes.

## Entra Redirects And API CORS

Each deployed UI origin and signed-out route must be registered on its matching
environment SPA app registration. Runtime redirect values must match those
registrations.

The API CORS allow-list must include the deployed origin without weakening API
authentication or authorization. CORS ownership and verification are documented
in [`../../api/configuration.md`](../../api/configuration.md).

## Cache And Upload Order

After the complete replacement package exists locally, the workflow clears the
static website and uploads in this order:

1. Versioned JavaScript, CSS, and assets with long-lived cache headers.
2. `config/app-config.json` with no-cache headers.
3. `index.html` with no-cache headers, uploaded last.

Uploading the HTML shell last reduces the chance that it references assets that
have not been uploaded yet.

The clear-before-upload step is an accepted current risk. If upload fails after
the clear, the site can be incomplete until a successful redeployment. Additive
cutover and stale-asset cleanup remain separate deployment-hardening work.

## Validation

Before relying on deployment, run the UI build and test commands documented in
[`../development/build-and-test.md`](../development/build-and-test.md).

After deployment, verify:

- The static website root serves the application.
- `config/app-config.json` is present and contains only public settings.
- A protected deep link returns the SPA shell and initiates sign-in when needed.
- The signed-out route renders correctly.
- Browser sign-in uses the deployed origin.
- Authenticated API calls are accepted by CORS and still enforce API `401` and
  `403` behavior.

## Rollback

For a bad application deployment, rerun the workflow from the last known-good
commit to the matching Environment. Prod rollback requires explicit `prod`
selection and required-reviewer approval. For bad runtime configuration,
correct the GitHub Environment value, replace only `config/app-config.json`,
and repeat the smoke checks. For an origin mistake, correct both the matching
SPA redirect registration and API CORS allow-list before redeploying.

The current workflows intentionally retain broad push triggers and concurrency
that includes the source ref. Component path filters and target-only
concurrency remain separate deployment-hardening work.

## Security

- Use least-privilege deployment identity scopes.
- Treat all browser runtime configuration as public.
- Keep environment-specific identifiers outside durable documentation.
- Do not print credentials or token-bearing values in workflow logs.
- Preserve API authorization as the enforcement boundary for protected data and
  operations.

