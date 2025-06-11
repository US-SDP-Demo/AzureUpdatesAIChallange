metadata description = 'Creates an Azure Cognitive Search service.'

@description('The name of the search service')
param name string

@description('Location for the search service')
param location string = resourceGroup().location

@description('Tags to apply to the search service')
param tags object = {}

@description('The pricing tier of the search service')
@allowed(['free', 'basic', 'standard', 'standard2', 'standard3', 'storage_optimized_l1', 'storage_optimized_l2'])
param sku string = 'basic'

@description('The number of replicas in the search service')
@minValue(1)
@maxValue(12)
param replicaCount int = 1

@description('The number of partitions in the search service')
param partitionCount int = 1

@description('Applicable only for the standard3 SKU. You can set this property to enable up to 3 high density partitions')
param hostingMode string = 'default'

@description('Principal ID to assign roles to')
param principalId string = ''

@description('Type of principal (User, Group, ServicePrincipal)')
param principalType string = 'User'

resource searchService 'Microsoft.Search/searchServices@2023-11-01' = {  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: sku
  }
  properties: {
    replicaCount: replicaCount
    partitionCount: partitionCount
    hostingMode: hostingMode
    publicNetworkAccess: 'enabled'
    encryptionWithCmk: {
      enforcement: 'Unspecified'
    }
    disableLocalAuth: false
    authOptions: {
      apiKeyOnly: {}
    }
  }
}

// Give the specified principal access to the search service
resource searchContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  scope: searchService
  name: guid(searchService.id, principalId, '7ca78c08-252a-4471-8644-bb5ff32d4ba0')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0')
    principalId: principalId
    principalType: principalType
  }
}

resource searchDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  scope: searchService
  name: guid(searchService.id, principalId, '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
    principalId: principalId
    principalType: principalType
  }
}

output id string = searchService.id
output name string = searchService.name
output endpoint string = 'https://${searchService.name}.search.windows.net'
