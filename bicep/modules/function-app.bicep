targetScope = 'resourceGroup'

param location string
param baseTags object
param functionPlanName string
param functionAppName string
param functionStorageAccountName string
param applicationStorageAccountName string
param deploymentStorageContainerName string
param applicationInsightsName string

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

@minValue(40)
@maxValue(1000)
param maximumInstanceCount int

@allowed([
  512
  2048
  4096
])
param instanceMemoryMB int

resource functionStorageAccount 'Microsoft.Storage/storageAccounts@2025-08-01' existing = {
  name: functionStorageAccountName
}

resource applicationStorageAccount 'Microsoft.Storage/storageAccounts@2025-08-01' existing = {
  name: applicationStorageAccountName
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

var functionStorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${functionStorageAccount.name};AccountKey=${functionStorageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
var storageTableDataContributorRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
)
var storageBlobDataContributorRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
)
var apiTags = union(baseTags, {
  Component: 'Api'
})

resource functionPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: functionPlanName
  location: location
  tags: apiTags
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionAppName
  location: location
  tags: apiTags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: functionPlan.id
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
    }
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${functionStorageAccount.properties.primaryEndpoints.blob}${deploymentStorageContainerName}'
          authentication: {
            type: 'StorageAccountConnectionString'
            storageAccountConnectionStringName: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
          }
        }
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
      scaleAndConcurrency: {
        maximumInstanceCount: maximumInstanceCount
        instanceMemoryMB: instanceMemoryMB
      }
      // The live 2024-04-01 Flex schema supports this property, but the current
      // Bicep type metadata omits it.
      #disable-next-line BCP037
      siteUpdateStrategy: {
        type: 'Recreate'
      }
    }
  }
}

resource applicationTableDataRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(
    applicationStorageAccount.id,
    functionApp.id,
    storageTableDataContributorRoleDefinitionId
  )
  scope: applicationStorageAccount
  properties: {
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: storageTableDataContributorRoleDefinitionId
  }
}

resource applicationBlobDataRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(
    applicationStorageAccount.id,
    functionApp.id,
    storageBlobDataContributorRoleDefinitionId
  )
  scope: applicationStorageAccount
  properties: {
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: storageBlobDataContributorRoleDefinitionId
  }
}

resource functionAppSettings 'Microsoft.Web/sites/config@2024-04-01' = {
  parent: functionApp
  name: 'appsettings'
  properties: {
    AzureWebJobsStorage: functionStorageConnectionString
    DEPLOYMENT_STORAGE_CONNECTION_STRING: functionStorageConnectionString
    AzureStorage__TableServiceUri: applicationStorageAccount.properties.primaryEndpoints.table
    AzureStorage__BlobServiceUri: applicationStorageAccount.properties.primaryEndpoints.blob
    AzureStorage__CreateResourcesIfMissing: 'false'
    AzureStorage__CalendarEventsTableName: 'CalendarEvents'
    AzureStorage__TemplatesTableName: 'Templates'
    AzureStorage__ApplicationSettingsTableName: 'ApplicationSettings'
    AzureStorage__PlatformsTableName: 'Platforms'
    AzureStorage__PlatformPublicationsTableName: 'PlatformPublications'
    AzureStorage__ThumbnailsContainerName: 'calendar-event-thumbnails'
    Auth__Instance: authInstance
    Auth__TenantId: authTenantId
    Auth__ClientId: authClientId
    Auth__Issuer: authIssuer
    Auth__RequiredAppRole: authRequiredAppRole
    PublicationExecution__OperationTimeoutSeconds: '${publicationOperationTimeoutSeconds}'
    PublicationExecution__FinalizationTimeoutSeconds: '${publicationFinalizationTimeoutSeconds}'
    PublicationExecution__StaleAfterSeconds: '${publicationStaleAfterSeconds}'
    APPLICATIONINSIGHTS_CONNECTION_STRING: applicationInsights.properties.ConnectionString
  }
}

output functionAppName string = functionApp.name
output functionAppUrl string = 'https://${functionApp.properties.defaultHostName}'
