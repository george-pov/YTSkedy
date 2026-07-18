targetScope = 'resourceGroup'

param location string

@allowed([
  'dev'
  'prod'
])
param environmentName string

param baseTags object
param functionStorageAccountName string
param applicationStorageAccountName string
param uiStorageAccountName string
param deploymentStorageContainerName string

var commonStorageProperties = {
  allowBlobPublicAccess: false
  allowCrossTenantReplication: false
  defaultToOAuthAuthentication: true
  minimumTlsVersion: 'TLS1_2'
  publicNetworkAccess: 'Enabled'
  supportsHttpsTrafficOnly: true
}

resource functionStorageAccount 'Microsoft.Storage/storageAccounts@2025-08-01' = {
  name: functionStorageAccountName
  location: location
  tags: union(baseTags, {
    Environment: environmentName
    Component: 'Function'
  })
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: union(commonStorageProperties, {
    allowSharedKeyAccess: true
  })
}

resource functionBlobService 'Microsoft.Storage/storageAccounts/blobServices@2025-08-01' = {
  parent: functionStorageAccount
  name: 'default'
  properties: {}
}

resource deploymentContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-08-01' = {
  parent: functionBlobService
  name: deploymentStorageContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource applicationStorageAccount 'Microsoft.Storage/storageAccounts@2025-08-01' = {
  name: applicationStorageAccountName
  location: location
  tags: union(baseTags, {
    Environment: environmentName
    Component: 'Data'
  })
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: union(commonStorageProperties, {
    allowSharedKeyAccess: false
  })
}

resource applicationTableService 'Microsoft.Storage/storageAccounts/tableServices@2025-08-01' = {
  parent: applicationStorageAccount
  name: 'default'
  properties: {}
}

resource calendarEventsTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2025-08-01' = {
  parent: applicationTableService
  name: 'CalendarEvents'
  properties: {}
}

resource templatesTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2025-08-01' = {
  parent: applicationTableService
  name: 'Templates'
  properties: {}
}

resource applicationSettingsTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2025-08-01' = {
  parent: applicationTableService
  name: 'ApplicationSettings'
  properties: {}
}

resource platformsTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2025-08-01' = {
  parent: applicationTableService
  name: 'Platforms'
  properties: {}
}

resource platformPublicationsTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2025-08-01' = {
  parent: applicationTableService
  name: 'PlatformPublications'
  properties: {}
}

resource applicationBlobService 'Microsoft.Storage/storageAccounts/blobServices@2025-08-01' = {
  parent: applicationStorageAccount
  name: 'default'
  properties: {}
}

resource thumbnailsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-08-01' = {
  parent: applicationBlobService
  name: 'calendar-event-thumbnails'
  properties: {
    publicAccess: 'None'
  }
}

resource uiStorageAccount 'Microsoft.Storage/storageAccounts@2025-08-01' = {
  name: uiStorageAccountName
  location: location
  tags: union(baseTags, {
    Environment: environmentName
    Component: 'Ui'
  })
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: union(commonStorageProperties, {
    allowSharedKeyAccess: false
  })
}

resource uiBlobService 'Microsoft.Storage/storageAccounts/blobServices@2025-08-01' = {
  parent: uiStorageAccount
  name: 'default'
  properties: {
    staticWebsite: {
      enabled: true
      indexDocument: 'index.html'
      errorDocument404Path: 'index.html'
    }
  }
}

output functionStorageAccountName string = functionStorageAccount.name
output applicationStorageAccountName string = applicationStorageAccount.name
output uiStorageAccountName string = uiStorageAccount.name
output deploymentContainerName string = deploymentStorageContainerName
output uiStaticWebsiteUrl string = uiStorageAccount.properties.primaryEndpoints.web
