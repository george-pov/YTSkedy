targetScope = 'resourceGroup'

param location string
param baseTags object
param logAnalyticsWorkspaceName string
param applicationInsightsName string
param actionGroupName string
param actionGroupShortName string
param failureAnomalyAlertName string

@secure()
param alertReceiverName string

@secure()
param alertReceiverEmailAddress string

@minValue(30)
@maxValue(730)
param logRetentionInDays int

var monitoringTags = union(baseTags, {
  Component: 'Monitoring'
})

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: monitoringTags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: logRetentionInDays
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  tags: monitoringTags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: actionGroupName
  location: 'global'
  tags: monitoringTags
  properties: {
    groupShortName: actionGroupShortName
    enabled: true
    emailReceivers: [
      {
        name: alertReceiverName
        emailAddress: alertReceiverEmailAddress
        useCommonAlertSchema: true
      }
    ]
  }
}

resource failureAnomalyAlert 'microsoft.alertsManagement/smartDetectorAlertRules@2021-04-01' = {
  name: failureAnomalyAlertName
  location: 'global'
  tags: monitoringTags
  properties: {
    description: 'Application Insights failure anomaly detection for YTSkedy.'
    state: 'Enabled'
    severity: 'Sev3'
    frequency: 'PT1M'
    detector: {
      id: 'FailureAnomaliesDetector'
    }
    scope: [
      applicationInsights.id
    ]
    actionGroups: {
      groupIds: [
        actionGroup.id
      ]
    }
  }
}

output logAnalyticsWorkspaceName string = logAnalyticsWorkspace.name
output applicationInsightsName string = applicationInsights.name
