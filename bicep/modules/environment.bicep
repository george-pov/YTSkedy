targetScope = 'resourceGroup'

param location string

@allowed([
  'dev'
  'prod'
])
param environmentName string

param baseTags object
param functionAppName string
param functionPlanName string
param functionStorageAccountName string
param applicationStorageAccountName string
param uiStorageAccountName string
param applicationInsightsName string
param logAnalyticsWorkspaceName string
param deploymentIdentityName string
param federatedCredentialName string
param actionGroupName string
param actionGroupShortName string
param failureAnomalyAlertName string
@maxLength(260)
param resourceGroupLockName string
param deploymentStorageContainerName string
param repositoryName string

@secure()
param authInstance string

@secure()
param authTenantId string

@secure()
param authClientId string

@secure()
param authIssuer string

param authRequiredAppRole string

@minValue(1)
param publicationOperationTimeoutSeconds int

@minValue(1)
param publicationFinalizationTimeoutSeconds int

@minValue(1)
param publicationStaleAfterSeconds int

@secure()
param alertReceiverName string

@secure()
param alertReceiverEmailAddress string

@minValue(30)
@maxValue(730)
param logRetentionInDays int

@minValue(40)
@maxValue(1000)
param maximumInstanceCount int

@allowed([
  512
  2048
  4096
])
param instanceMemoryMB int

param enableResourceGroupDeleteLock bool

module storage './storage.bicep' = {
  name: 'storage-${environmentName}'
  params: {
    location: location
    environmentName: environmentName
    baseTags: baseTags
    functionStorageAccountName: functionStorageAccountName
    applicationStorageAccountName: applicationStorageAccountName
    uiStorageAccountName: uiStorageAccountName
    deploymentStorageContainerName: deploymentStorageContainerName
  }
}

module monitoring './monitoring.bicep' = {
  name: 'monitoring-${environmentName}'
  params: {
    location: location
    baseTags: baseTags
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
    applicationInsightsName: applicationInsightsName
    actionGroupName: actionGroupName
    actionGroupShortName: actionGroupShortName
    failureAnomalyAlertName: failureAnomalyAlertName
    alertReceiverName: alertReceiverName
    alertReceiverEmailAddress: alertReceiverEmailAddress
    logRetentionInDays: logRetentionInDays
  }
}

module functionApp './function-app.bicep' = {
  name: 'function-app-${environmentName}'
  params: {
    location: location
    baseTags: baseTags
    functionPlanName: functionPlanName
    functionAppName: functionAppName
    functionStorageAccountName: functionStorageAccountName
    applicationStorageAccountName: applicationStorageAccountName
    deploymentStorageContainerName: deploymentStorageContainerName
    applicationInsightsName: applicationInsightsName
    authInstance: authInstance
    authTenantId: authTenantId
    authClientId: authClientId
    authIssuer: authIssuer
    authRequiredAppRole: authRequiredAppRole
    publicationOperationTimeoutSeconds: publicationOperationTimeoutSeconds
    publicationFinalizationTimeoutSeconds: publicationFinalizationTimeoutSeconds
    publicationStaleAfterSeconds: publicationStaleAfterSeconds
    maximumInstanceCount: maximumInstanceCount
    instanceMemoryMB: instanceMemoryMB
  }
  dependsOn: [
    storage
    monitoring
  ]
}

module deploymentIdentity './deployment-identity.bicep' = {
  name: 'deployment-identity-${environmentName}'
  params: {
    location: location
    baseTags: baseTags
    environmentName: environmentName
    repositoryName: repositoryName
    deploymentIdentityName: deploymentIdentityName
    federatedCredentialName: federatedCredentialName
    functionAppName: functionAppName
    uiStorageAccountName: uiStorageAccountName
  }
  dependsOn: [
    functionApp
    storage
  ]
}

resource resourceGroupDeleteLock 'Microsoft.Authorization/locks@2020-05-01' = if (enableResourceGroupDeleteLock) {
  name: resourceGroupLockName
  properties: {
    level: 'CanNotDelete'
    notes: 'Prevents accidental deletion of the YTSkedy production environment.'
  }
  dependsOn: [
    deploymentIdentity
  ]
}

output functionAppName string = functionApp.outputs.functionAppName
output functionAppUrl string = functionApp.outputs.functionAppUrl
output uiStorageAccountName string = storage.outputs.uiStorageAccountName
output uiStaticWebsiteUrl string = storage.outputs.uiStaticWebsiteUrl
output deploymentIdentityClientId string = deploymentIdentity.outputs.deploymentIdentityClientId
output deploymentIdentityPrincipalId string = deploymentIdentity.outputs.deploymentIdentityPrincipalId
output applicationInsightsName string = monitoring.outputs.applicationInsightsName
output logAnalyticsWorkspaceName string = monitoring.outputs.logAnalyticsWorkspaceName
