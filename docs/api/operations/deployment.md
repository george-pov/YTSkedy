# API Deployment

YTSkedy deploys the backend Azure Functions host to separate `dev` and `prod`
Azure environments with GitHub Actions.

## Workflow

The deployment workflow is:

```text
.github/workflows/deploy-azure-function.yml
```

The workflow runs on pushes to `main` and can also be started manually. Pushes
target the `dev` GitHub Environment. A prod deployment requires manual
selection of the protected `prod` Environment and its required approval.

The workflow restores the backend solution, builds it, runs backend tests,
publishes `YTSkedy.AzureFunctions`, and deploys only after those steps pass.

The workflow path variables point at the backend workspace under `src/api/`.
It does not install npm packages, build the Angular app, publish frontend
assets, or deploy a frontend host.

Infrastructure deployment is separate from application deployment. The
dedicated infrastructure workflow performs source validation and supports
manually dispatched Azure validation, what-if, and apply operations. Its Bicep
entry point, environment secrets, identity bootstrap, and approval process are
documented in
[`../../operations/azure-environments.md`](../../operations/azure-environments.md).

## GitHub Environment

The workflow uses OpenID Connect authentication with Azure. Do not store
publish profiles, client secrets, storage connection strings, OAuth tokens, or
API keys in the repository.

Configure GitHub Environments named `dev` and `prod`. Each contains these
non-secret variables with values for only that environment:

```text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
AZURE_FUNCTIONAPP_NAME
```

`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` identify the
environment-specific Azure user-assigned managed identity used by the workflow.
`AZURE_FUNCTIONAPP_NAME` is the matching Azure Function App name.

Do not copy a dev Function name, identity, tenant, or subscription value into
prod. Concrete values belong in GitHub and local operational records, not this
document.

## Azure Identity Setup

Configure a separate federated credential on each environment's Azure
user-assigned managed identity.

Because the deploy job declares a GitHub Environment, the default GitHub OIDC
subject uses the environment name:

```text
repo:OWNER/REPOSITORY:environment:<environment>
```

Use `dev` or `prod` as the environment segment. Assign Website Contributor only
at the matching Function App scope. Do not grant the deployment identity
Contributor at subscription or resource-group scope.

Runtime settings required by the function app, including
`AzureWebJobsStorage`, `DEPLOYMENT_STORAGE_CONNECTION_STRING`,
`AzureStorage:TableServiceUri`, and `AzureStorage:BlobServiceUri`, belong in
Azure Function App configuration, not in the workflow file. Application
storage uses the Function App system-assigned identity; no application storage
connection string is deployed. The exact hosted setting contract and separate
Function host versus application data storage ownership are documented in
[`../configuration.md`](../configuration.md).

## Prod Promotion And Rollback

Promote an exact commit that has already deployed successfully to dev. Start
both the Function and UI workflows manually with `prod` selected and wait for
the configured reviewer approval. A push to `main` must not target prod.

For rollback, manually dispatch the last known-good commit to the matching
Environment. Prod rollback remains protected and requires the same explicit
selection and approval as forward promotion.

The current workflow intentionally retains broad push triggering and
source-ref-based concurrency. Component path filters and target-only
concurrency remain separate deployment-hardening work.
