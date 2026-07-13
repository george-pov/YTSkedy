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
var applicationStorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${applicationStorageAccount.name};AccountKey=${applicationStorageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
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

resource functionAppSettings 'Microsoft.Web/sites/config@2024-04-01' = {
  parent: functionApp
  name: 'appsettings'
  properties: {
    AzureWebJobsStorage: functionStorageConnectionString
    DEPLOYMENT_STORAGE_CONNECTION_STRING: functionStorageConnectionString
    AzureStorage__ConnectionString: applicationStorageConnectionString
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
    APPLICATIONINSIGHTS_CONNECTION_STRING: applicationInsights.properties.ConnectionString
  }
}

output functionAppName string = functionApp.name
output functionAppUrl string = 'https://${functionApp.properties.defaultHostName}'
