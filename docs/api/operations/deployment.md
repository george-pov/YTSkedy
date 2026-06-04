# API Deployment

YTSkedy currently deploys the backend Azure Functions host with GitHub Actions.

## Workflow

The deployment workflow is:

```text
.github/workflows/deploy-azure-function.yml
```

The workflow runs on pushes to `main` and can also be started manually. It
restores the backend solution, builds the backend solution, runs the configured
backend unit test project, publishes `YTSkedy.AzureFunctions`, and deploys only
after those steps pass.

The workflow path variables point at the backend workspace under `src/api/`.
It does not install npm packages, build the Angular app, publish frontend
assets, or deploy a frontend host.

## GitHub Environment

The workflow uses OpenID Connect authentication with Azure. Do not store
publish profiles, client secrets, storage connection strings, OAuth tokens, or
API keys in the repository.

Create a GitHub Environment named `production`, or select another environment
name when manually running the workflow. Add these GitHub Environment
variables:

```text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
AZURE_FUNCTIONAPP_NAME
```

`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` identify the
Azure user-assigned managed identity used by the workflow.
`AZURE_FUNCTIONAPP_NAME` is the existing Azure Function App name.

## Azure Identity Setup

Configure a federated credential on the Azure user-assigned managed identity
for each GitHub Environment that can deploy.

Because the deploy job declares a GitHub Environment, the default GitHub OIDC
subject uses the environment name:

```text
repo:OWNER/REPO:environment:production
```

Replace `OWNER/REPO` and `production` with the repository and environment that
will deploy. Assign the managed identity a deployment role scoped to the target
Function App, such as Website Contributor.

Runtime settings required by the function app, including
`AzureWebJobsStorage` or `AzureStorage:ConnectionString`, belong in Azure
Function App configuration, not in the workflow file.
