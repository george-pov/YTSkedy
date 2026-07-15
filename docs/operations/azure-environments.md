# Azure Environments

Portable operations guidance for the YTSkedy Azure `dev` and `prod`
environments. Keep concrete tenant, subscription, client, resource, receiver,
and deployed URL values outside durable documentation.

## Ownership

- Source of truth: environment shape, Bicep layout, deployment inputs,
  dev-first workflow, protected prod promotion, validation, recovery, and
  destructive approval boundaries.
- Update when: the environment model, Bicep modules, deployment scripts,
  GitHub Environment contract, identity permissions, lock behavior, or
  operational validation changes.
- Excludes: concrete deployed identifiers, credentials, local validation
  evidence, provider setup values, and environment-specific recovery notes.

## Environment Model

Both environments use one Bicep entry point and the same modules. Differences
are supplied by tracked parameter files or process-scoped deployment inputs.

| Behavior | Dev | Prod |
| --- | --- | --- |
| GitHub Environment | `dev` | `prod` |
| Application deployment | Pushes to `main`; explicit manual dispatch | Explicit manual dispatch only |
| Infrastructure deployment | Manual workflow dispatch after reviewed what-if | Manual workflow dispatch after reviewed what-if and required approval |
| Deployment protection | Exact `main` branch policy before infrastructure secrets are added | Required reviewer and exact `main` branch policy |
| Log retention | 30 days | 90 days |
| Resource-group delete lock | Disabled | `CanNotDelete`, enabled only after validation |
| Application data | Separate storage | Separate storage |

Do not share storage accounts, monitoring resources, managed identities,
federated credentials, role assignments, Entra app registrations, runtime
configuration, or application data between environments.

## Bicep Layout

The subscription-scope entry point is:

```text
bicep/main.bicep
```

The infrastructure workflow is:

```text
.github/workflows/deploy-azure-infrastructure.yml
```

Tracked environment parameters are:

```text
bicep/environments/dev.bicepparam
bicep/environments/prod.bicepparam
```

The entry point creates the environment resource group and invokes one
resource-group-scoped composition module. The modules own:

- Three storage accounts: Function host and deployment storage, application
  data storage, and UI static website storage.
- A Flex Consumption plan and .NET isolated Azure Functions app.
- Log Analytics, Application Insights, an action group, and failure anomaly
  detection.
- One environment-specific deployment identity, GitHub OIDC credential, and
  resource-scoped deployment roles.
- The conditional prod resource-group delete lock.

Azure deployments use incremental behavior. Do not switch this process to
complete mode.

## Deployment Inputs

Public resource names, location, tags, retention, runtime scale, and lock state
belong in the tracked environment parameter files.

Authentication and alert receiver inputs come from the PowerShell process that
runs validation or deployment. For GitHub Actions, Environment secrets are
mapped into that process only for the infrastructure job. The parameter files
read these six variables through an environment-specific `YTSKEDY_DEV_` or
`YTSKEDY_PROD_` prefix:

```text
AUTH_INSTANCE
AUTH_TENANT_ID
AUTH_CLIENT_ID
AUTH_ISSUER
ALERT_RECEIVER_NAME
ALERT_RECEIVER_EMAIL_ADDRESS
```

Bicep marks all six values secure so they are not retained in normal deployment
history. Do not place their values in command history, tracked parameter files,
generated templates, GitHub variables, or durable documentation. Store the
GitHub copies as Environment secrets. Remove local process values when the
operation ends.

Provider credentials are not infrastructure deployment inputs. Configure them
only through the application-owned platform settings flow.

## Local Validation And Deployment

Run commands from the repository root. Select one environment and populate its
six process variables before invoking the wrapper:

```powershell
$environment = 'dev'

./scripts/azure/Test-AzureEnvironmentNames.ps1 `
  -Environment $environment

./scripts/azure/Deploy-AzureEnvironment.ps1 `
  -Environment $environment `
  -ValidateOnly

./scripts/azure/Deploy-AzureEnvironment.ps1 `
  -Environment $environment `
  -WhatIf
```

Review the complete what-if before applying. It must target only the selected
environment and must not contain an unexplained delete or replacement.

Apply is a separate live mutation and requires interactive confirmation:

```powershell
./scripts/azure/Deploy-AzureEnvironment.ps1 `
  -Environment $environment `
  -Apply
```

The wrapper always repeats name ownership checks, Bicep build, subscription
validation, and what-if before the confirmation prompt. It prints only an
approved set of public deployment outputs.

## Automated Infrastructure Workflow

Pull requests and pushes that change `bicep/`, `scripts/azure/`, or the
infrastructure workflow compile the Bicep entry point and both parameter files.
This source-validation job uses inert values, does not log in to Azure, and
cannot apply infrastructure.

Live operations use manual dispatch from `main`. Dispatches from any other ref
run source validation only and cannot start the secret-bearing deployment job.
Select the GitHub Environment and one operation:

- `validate` runs Azure subscription deployment validation.
- `what-if` reports resource ids and change types without applying them.
- `apply` repeats validation and what-if, then performs the incremental
  deployment.

Run and review `what-if` before starting a separate `apply` run. Apply requires
`apply dev` or `apply prod` in the confirmation input. The deployment job
references the selected GitHub Environment, so its branch rules, required
reviewers, variables, and secrets are enforced before Azure login. The script
permits noninteractive apply only when GitHub Actions and the selected
Environment match.

### GitHub Environment Infrastructure Inputs

Each `dev` and `prod` GitHub Environment requires these additional values:

| Name | Classification | Purpose |
| --- | --- | --- |
| `AZURE_INFRA_CLIENT_ID` | Variable | Client id of the dedicated infrastructure deployment identity. |
| `BICEP_AUTH_INSTANCE` | Secret | Matching External ID authority instance. |
| `BICEP_AUTH_TENANT_ID` | Secret | Matching External ID tenant id. |
| `BICEP_AUTH_CLIENT_ID` | Secret | Matching API app registration client id. |
| `BICEP_AUTH_ISSUER` | Secret | Exact matching user-flow OpenID metadata issuer. |
| `BICEP_ALERT_RECEIVER_NAME` | Secret | Matching action group receiver name. |
| `BICEP_ALERT_RECEIVER_EMAIL_ADDRESS` | Secret | Matching action group receiver address. |

The workflow reuses the existing `AZURE_TENANT_ID` and
`AZURE_SUBSCRIPTION_ID` variables. `AZURE_INFRA_CLIENT_ID` must not replace the
application deployment `AZURE_CLIENT_ID` value.

Before adding the secrets, configure both Environments to allow deployments
only from the exact `main` branch. Keep the existing required reviewer on
`prod`. Environment secrets are released to a job only after its deployment
branch and protection rules pass.

Set each secret through an interactive prompt so its value is not written to
shell history:

```powershell
$environment = 'dev'

gh secret set BICEP_AUTH_INSTANCE --env $environment
gh secret set BICEP_AUTH_TENANT_ID --env $environment
gh secret set BICEP_AUTH_CLIENT_ID --env $environment
gh secret set BICEP_AUTH_ISSUER --env $environment
gh secret set BICEP_ALERT_RECEIVER_NAME --env $environment
gh secret set BICEP_ALERT_RECEIVER_EMAIL_ADDRESS --env $environment
```

Repeat for `prod` with only prod values. Authentication values come from the
matching External ID API registration and user-flow OpenID metadata. Receiver
values come from the matching Azure Monitor action group configuration.

### Infrastructure Identity Bootstrap

Infrastructure automation uses a dedicated user-assigned managed identity per
environment. Do not broaden the application deployment identity. The current
entry point creates a resource group at subscription scope and creates Azure
role assignments, so the infrastructure identity requires Contributor and
Role Based Access Control Administrator at the deployment subscription. These
are privileged assignments. Create them once through an Azure administrator,
keep the workflow manual, and preserve prod Environment review protection.

The following is a bootstrap template. Replace placeholders, review the active
Azure subscription, and treat execution as a separately approved live Azure
and GitHub mutation:

```powershell
$environment = 'dev'
$resourceGroup = '<environment-resource-group>'
$identityName = "id-ytskedy-infrastructure-$environment"
$repository = '<owner>/<repository>'
$subscriptionId = az account show --query id --output tsv
$subscriptionScope = "/subscriptions/$subscriptionId"

$identity = az identity create `
  --name $identityName `
  --resource-group $resourceGroup `
  --output json | ConvertFrom-Json

az identity federated-credential create `
  --name "github-infrastructure-$environment" `
  --identity-name $identityName `
  --resource-group $resourceGroup `
  --issuer 'https://token.actions.githubusercontent.com' `
  --subject "repo:${repository}:environment:$environment" `
  --audiences 'api://AzureADTokenExchange' `
  --output none

az role assignment create `
  --assignee-object-id $identity.principalId `
  --assignee-principal-type ServicePrincipal `
  --role 'b24988ac-6180-42a0-ab88-20f7382dd24c' `
  --scope $subscriptionScope `
  --output none

az role assignment create `
  --assignee-object-id $identity.principalId `
  --assignee-principal-type ServicePrincipal `
  --role 'f58310d9-a9f6-439a-9e8d-f62e7b41a168' `
  --scope $subscriptionScope `
  --output none

gh variable set AZURE_INFRA_CLIENT_ID `
  --env $environment `
  --body $identity.clientId
```

The first role id is Contributor. The second is Role Based Access Control
Administrator. Repeat the bootstrap with a separate identity for `prod`.

## GitHub Environments And Promotion

The infrastructure, Function, and UI workflows use GitHub Environments named
`dev` and `prod`. They use GitHub OIDC and do not require an Azure client
secret.

Pushes to `main` resolve to `dev`. A prod deployment must be started manually
with `prod` selected and must pass the prod Environment reviewer and branch
policy. Promote an exact commit that has already completed dev deployment and
validation.

Each application or infrastructure environment identity has one federated
subject:

```text
repo:OWNER/REPOSITORY:environment:<environment>
```

The application deployment identity receives only:

- Website Contributor scoped to the matching Function App.
- Storage Blob Data Contributor scoped to the matching UI storage account.

Do not grant the application deployment identity Contributor at subscription
or resource-group scope. The separately bootstrapped infrastructure identity
owns the privileged subscription deployment path.

## Manual Platform Boundaries

Entra External ID app registrations and Azure Functions platform CORS are not
managed by Bicep.

Each environment requires separate API and SPA registrations, API scopes, app
role assignments, SPA redirects, consent, and user-flow association. Browser
runtime configuration must reference only the matching environment.

CORS must be configured on the matching Function App. Dev allows the approved
local origins and its deployed UI origin. Prod allows only its deployed UI
origin. `supportCredentials` remains `false`, and wildcard origins are not
allowed.

## Post-Deployment Validation

Validate each environment independently:

- Resource inventory, required tags, provisioning state, and module parity.
- Separate storage, monitoring, identity, OIDC subject, and role scopes.
- Exact Function runtime and app-setting names without printing values.
- SPA redirects, API scopes, app role, token audience and issuer, and browser
  sign-in and sign-out.
- Platform CORS allow-list and rejected cross-environment origins.
- UI root, runtime configuration, protected routes, and unauthenticated API
  boundary.
- Disposable create, read, update, thumbnail, and delete behavior when a live
  data-write approval exists.
- Application Insights and Log Analytics requests and storage dependencies.
- Zero disposable records after validation.

Do not copy dev data or provider credentials into prod as part of promotion.

## Delete Lock And Repair

Prod is validated before its resource-group `CanNotDelete` lock is enabled.
The tracked prod parameter keeps the lock enabled after that gate.

For an approved infrastructure repair that the lock blocks:

1. Record the repair scope and review a prod what-if.
2. Explicitly delete the lock using the concrete values from the local
   operations inventory.
3. Perform the approved repair through an incremental deployment.
4. Rerun prod validation.
5. Apply the tracked prod parameter again and verify the lock is restored.

Never test the lock by issuing a resource-group deletion. Verify its exact
scope and `CanNotDelete` level through ARM.

## Rollback

For an application regression, manually dispatch both workflows from the last
known-good commit to the matching GitHub Environment. Prod rollback still
requires explicit `prod` selection and reviewer approval.

For infrastructure drift, use another reviewed incremental Bicep deployment.
Do not delete an environment resource group unless a separate destructive
approval names the target and data disposition.

The retired legacy environment was deleted after explicit approval with no
backup. That data cannot be recovered. Bicep can recreate infrastructure but
cannot recreate deleted application data or provider state.

## Current Limitations

- The infrastructure configures alert routing, but independent email-delivery
  verification is an operator responsibility.
- Application-data backup, budget controls, custom domains, and disaster
  recovery are not configured by this deployment source.
- Provider credentials and provider-side recovery remain application and
  operator concerns.
- The current UI workflow clears the static website before replacement upload.
  A failed upload can require an immediate last-known-good redeployment.
- Workflow path filtering and target-only concurrency hardening remain separate
  deployment workflow work.
