targetScope = 'subscription'

@allowed([
  'dev'
  'prod'
])
param environmentName string

@minLength(1)
param location string

@minLength(1)
@maxLength(90)
param resourceGroupName string

@minLength(2)
@maxLength(60)
param functionAppName string

@minLength(1)
@maxLength(60)
param functionPlanName string

@minLength(3)
@maxLength(24)
param functionStorageAccountName string

@minLength(3)
@maxLength(24)
param applicationStorageAccountName string

@minLength(3)
@maxLength(24)
param uiStorageAccountName string

@minLength(1)
@maxLength(260)
param applicationInsightsName string

@minLength(4)
@maxLength(63)
param logAnalyticsWorkspaceName string

@minLength(3)
@maxLength(128)
param deploymentIdentityName string

@minLength(3)
@maxLength(120)
param federatedCredentialName string

@minLength(1)
@maxLength(260)
param actionGroupName string

@minLength(1)
@maxLength(12)
param actionGroupShortName string

@minLength(1)
@maxLength(260)
param failureAnomalyAlertName string

@maxLength(260)
param resourceGroupLockName string = ''

@minLength(3)
@maxLength(63)
param deploymentStorageContainerName string

@minLength(3)
@maxLength(100)
param repositoryName string

@secure()
@minLength(1)
param authInstance string

@secure()
@minLength(1)
param authTenantId string

@secure()
@minLength(1)
param authClientId string

@secure()
@minLength(1)
param authIssuer string

@minLength(1)
@maxLength(256)
param authRequiredAppRole string

@minValue(1)
param publicationOperationTimeoutSeconds int

@minValue(1)
param publicationFinalizationTimeoutSeconds int

@minValue(1)
param publicationStaleAfterSeconds int

@secure()
@minLength(1)
@maxLength(256)
param alertReceiverName string

@secure()
@minLength(3)
@maxLength(254)
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

var environmentTags = {
  Component: 'Environment'
}
var baseTags = union({
  Application: 'YTSkedy'
  Environment: environmentName
  Region: location
  ManagedBy: 'Bicep'
  Repository: repositoryName
}, environmentTags)

resource environmentResourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: resourceGroupName
  location: location
  tags: baseTags
}

module environmentDeployment './modules/environment.bicep' = {
  name: 'ytskedy-${environmentName}'
  scope: environmentResourceGroup
  params: {
    location: location
    environmentName: environmentName
    baseTags: baseTags
    functionAppName: functionAppName
    functionPlanName: functionPlanName
    functionStorageAccountName: functionStorageAccountName
    applicationStorageAccountName: applicationStorageAccountName
    uiStorageAccountName: uiStorageAccountName
    applicationInsightsName: applicationInsightsName
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
    deploymentIdentityName: deploymentIdentityName
    federatedCredentialName: federatedCredentialName
    actionGroupName: actionGroupName
    actionGroupShortName: actionGroupShortName
    failureAnomalyAlertName: failureAnomalyAlertName
    resourceGroupLockName: resourceGroupLockName
    deploymentStorageContainerName: deploymentStorageContainerName
    repositoryName: repositoryName
    authInstance: authInstance
    authTenantId: authTenantId
    authClientId: authClientId
    authIssuer: authIssuer
    authRequiredAppRole: authRequiredAppRole
    publicationOperationTimeoutSeconds: publicationOperationTimeoutSeconds
    publicationFinalizationTimeoutSeconds: publicationFinalizationTimeoutSeconds
    publicationStaleAfterSeconds: publicationStaleAfterSeconds
    alertReceiverName: alertReceiverName
    alertReceiverEmailAddress: alertReceiverEmailAddress
    logRetentionInDays: logRetentionInDays
    maximumInstanceCount: maximumInstanceCount
    instanceMemoryMB: instanceMemoryMB
    enableResourceGroupDeleteLock: enableResourceGroupDeleteLock
  }
}

output resourceGroupName string = environmentResourceGroup.name
output functionAppName string = environmentDeployment.outputs.functionAppName
output functionAppUrl string = environmentDeployment.outputs.functionAppUrl
output uiStorageAccountName string = environmentDeployment.outputs.uiStorageAccountName
output uiStaticWebsiteUrl string = environmentDeployment.outputs.uiStaticWebsiteUrl
output deploymentIdentityClientId string = environmentDeployment.outputs.deploymentIdentityClientId
output deploymentIdentityPrincipalId string = environmentDeployment.outputs.deploymentIdentityPrincipalId
output applicationInsightsName string = environmentDeployment.outputs.applicationInsightsName
output logAnalyticsWorkspaceName string = environmentDeployment.outputs.logAnalyticsWorkspaceName
