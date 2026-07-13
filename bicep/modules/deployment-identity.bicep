targetScope = 'resourceGroup'

param location string
param baseTags object

@allowed([
  'dev'
  'prod'
])
param environmentName string

param repositoryName string
param deploymentIdentityName string
param federatedCredentialName string
param functionAppName string
param uiStorageAccountName string

// Microsoft built-in Website Contributor role. Its public GUID is stable across subscriptions.
// subscriptionResourceId resolves the role in the deployment subscription at runtime.
var websiteContributorRoleDefinitionResourceId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'de139f84-1756-47ae-9be6-808fbbe84772'
)
// Microsoft built-in Storage Blob Data Contributor role. Its public GUID is stable across subscriptions.
// subscriptionResourceId resolves the role in the deployment subscription at runtime.
var storageBlobDataContributorRoleDefinitionResourceId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
)
var deploymentTags = union(baseTags, {
  Component: 'Deployment'
})

resource functionApp 'Microsoft.Web/sites@2024-04-01' existing = {
  name: functionAppName
}

resource uiStorageAccount 'Microsoft.Storage/storageAccounts@2025-08-01' existing = {
  name: uiStorageAccountName
}

resource deploymentIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: deploymentIdentityName
  location: location
  tags: deploymentTags
}

resource githubEnvironmentCredential 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: deploymentIdentity
  name: federatedCredentialName
  properties: {
    issuer: 'https://token.actions.githubusercontent.com'
    subject: 'repo:${repositoryName}:environment:${environmentName}'
    audiences: [
      'api://AzureADTokenExchange'
    ]
  }
}

resource functionDeploymentRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(functionApp.id, deploymentIdentity.id, websiteContributorRoleDefinitionResourceId)
  scope: functionApp
  properties: {
    roleDefinitionId: websiteContributorRoleDefinitionResourceId
    principalId: deploymentIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource uiDeploymentRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(uiStorageAccount.id, deploymentIdentity.id, storageBlobDataContributorRoleDefinitionResourceId)
  scope: uiStorageAccount
  properties: {
    roleDefinitionId: storageBlobDataContributorRoleDefinitionResourceId
    principalId: deploymentIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

output deploymentIdentityName string = deploymentIdentity.name
output deploymentIdentityClientId string = deploymentIdentity.properties.clientId
output deploymentIdentityPrincipalId string = deploymentIdentity.properties.principalId
