targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string

// Optional parameters to override the default azd resource naming conventions.
// Add the following to main.parameters.json to provide values:
// "resourceGroupName": {
//      "value": "myGroupName"
// }
param resourceGroupName string = ''

param webServiceName string = 'web'
param webContainerAppName string = ''

// Azure Search parameters
@description('The name of the Azure Search service')
param searchServiceName string = ''

@description('The SKU of the Azure Search service')
@allowed(['free', 'basic', 'standard', 'standard2', 'standard3', 'storage_optimized_l1', 'storage_optimized_l2'])
param searchServiceSku string = 'basic'

// OpenAI parameters
@description('The name of the Azure OpenAI service')
param openAIServiceName string = ''

@description('The location for the Azure OpenAI service')
param openAILocation string = 'eastus'

@description('The SKU for the Azure OpenAI service')
param openAISku string = 'S0'

@description('The deployment name for the OpenAI model')
param openAIDeploymentName string = 'gpt-4o-mini'

@description('The model name for the OpenAI deployment')
param openAIModelName string = 'gpt-4o-mini'

@description('The version of the OpenAI model')
param openAIModelVersion string = '2024-07-18'

@description('The capacity for the OpenAI deployment')
param openAIDeploymentCapacity int = 30

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

// The application frontend
module web './core/host/container-app-upsert.bicep' = {
  name: 'web'
  scope: rg
  params: {
    name: !empty(webContainerAppName) ? webContainerAppName : '${abbrs.appContainerApps}web-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': webServiceName })
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistry.outputs.name
    exists: false
    identityName: webIdentity.outputs.name
    identityType: 'UserAssigned'
    imageName: '${containerRegistry.outputs.loginServer}/simple-feed-reader:latest'
    ingressEnabled: true
    external: true
    targetPort: 5000
    env: [
      {
        name: 'AZURE_CLIENT_ID'
        value: webIdentity.outputs.clientId
      }
      {
        name: 'AzureOpenAIDeploymentName'
        value: openAIDeploymentName
      }
      {
        name: 'AzureOpenAIEndpoint'
        value: openai.outputs.endpoint
      }
      {
        name: 'AzureSearchEndpoint'
        value: search.outputs.endpoint
      }
      {
        name: 'AzureSearchIndexName'
        value: 'rss-feeds'
      }
    ]
    secrets: []
  }
}

module webIdentity './core/security/managed-identity.bicep' = {
  name: 'web-identity'
  scope: rg
  params: {
    name: '${abbrs.managedIdentityUserAssignedIdentities}web-${resourceToken}'
    location: location
    tags: tags
  }
}

// Container apps host (including container registry)
module containerAppsEnvironment './core/host/container-apps-environment.bicep' = {
  name: 'container-apps-environment'
  scope: rg
  params: {
    name: '${abbrs.appManagedEnvironments}${resourceToken}'
    location: location
    tags: tags
    logAnalyticsWorkspaceName: monitoring.outputs.logAnalyticsWorkspaceName
  }
}

module containerRegistry './core/host/container-registry.bicep' = {
  name: 'container-registry'
  scope: rg
  params: {
    name: '${abbrs.containerRegistryRegistries}${resourceToken}'
    location: location
    tags: tags
  }
}

// Azure Search service
module search './core/search/search-service.bicep' = {
  name: 'search-service'
  scope: rg
  params: {
    name: !empty(searchServiceName) ? searchServiceName : '${abbrs.searchSearchServices}${resourceToken}'
    location: location
    tags: tags
    sku: searchServiceSku
    principalId: principalId
    principalType: 'User'
  }
}

// Azure OpenAI service
module openai './core/ai/cognitiveservices.bicep' = {
  name: 'openai'
  scope: rg
  params: {
    name: !empty(openAIServiceName) ? openAIServiceName : '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    location: openAILocation
    tags: tags
    sku: {
      name: openAISku
    }
    kind: 'OpenAI'
    deployments: [
      {
        name: openAIDeploymentName
        model: {
          format: 'OpenAI'
          name: openAIModelName
          version: openAIModelVersion
        }
        capacity: openAIDeploymentCapacity
      }
    ]
    principalId: principalId
    principalType: 'User'
  }
}

// Give the API access to OpenAI
module openaiRoleWeb './core/security/role.bicep' = {
  name: 'openai-role-web'
  scope: rg
  params: {
    principalId: webIdentity.outputs.principalId
    roleDefinitionId: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd' // Cognitive Services OpenAI User
    principalType: 'ServicePrincipal'
  }
}

// Give the API access to Azure Search
module searchRoleWeb './core/security/role.bicep' = {
  name: 'search-role-web'
  scope: rg
  params: {
    principalId: webIdentity.outputs.principalId
    roleDefinitionId: '8ebe5a00-799e-43f5-93ac-243d3dce84a7' // Search Index Data Contributor
    principalType: 'ServicePrincipal'
  }
}

// Give the API access to Container Registry
module acrRoleWeb './core/security/role.bicep' = {
  name: 'acr-role-web'
  scope: rg
  params: {
    principalId: webIdentity.outputs.principalId
    roleDefinitionId: '7f951dda-4ed3-4680-a7ca-43fe172d538d' // AcrPull
    principalType: 'ServicePrincipal'
  }
}

// Monitor application with Azure Monitor
module monitoring './core/monitor/monitoring.bicep' = {
  name: 'monitoring'
  scope: rg
  params: {
    location: location
    tags: tags
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
  }
}

// Data outputs
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_RESOURCE_GROUP string = rg.name

// Container outputs
output AZURE_CONTAINER_ENVIRONMENT_NAME string = containerAppsEnvironment.outputs.name
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.outputs.loginServer
output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.outputs.name

// Application outputs
output SERVICE_WEB_IDENTITY_PRINCIPAL_ID string = webIdentity.outputs.principalId
output SERVICE_WEB_NAME string = web.outputs.name
output SERVICE_WEB_URI string = web.outputs.uri
output SERVICE_WEB_IMAGE_NAME string = web.outputs.imageName

// Azure Search outputs
output AZURE_SEARCH_SERVICE_NAME string = search.outputs.name
output AZURE_SEARCH_ENDPOINT string = search.outputs.endpoint

// Azure OpenAI outputs  
output AZURE_OPENAI_SERVICE_NAME string = openai.outputs.name
output AZURE_OPENAI_ENDPOINT string = openai.outputs.endpoint
output AZURE_OPENAI_DEPLOYMENT_NAME string = openAIDeploymentName
