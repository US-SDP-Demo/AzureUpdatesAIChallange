metadata description = 'Creates monitoring resources including Log Analytics workspace and Application Insights.'

@description('The name of the Log Analytics workspace')
param logAnalyticsName string

@description('The name of the Application Insights resource')
param applicationInsightsName string

@description('Location for the monitoring resources')
param location string = resourceGroup().location

@description('Tags to apply to the monitoring resources')
param tags object = {}

@description('The service tier of the workspace')
@allowed(['Free', 'Standalone', 'PerNode', 'PerGB2018'])
param skuName string = 'PerGB2018'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: skuName
    }
    retentionInDays: 30
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString
output applicationInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
output applicationInsightsName string = applicationInsights.name
output logAnalyticsWorkspaceId string = logAnalytics.id
output logAnalyticsWorkspaceName string = logAnalytics.name
