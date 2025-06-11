metadata description = 'Creates a Cognitive Services instance.'

@description('The name of the Cognitive Services resource')
param name string

@description('Location for the Cognitive Services resource')
param location string = resourceGroup().location

@description('Tags to apply to the Cognitive Services resource')
param tags object = {}

@description('The custom subdomain name used to access the API. Defaults to the value of the name parameter.')
param customSubDomainName string = name

@description('Deploy Cognitive Services to a Virtual Network.')
param deployToVnet bool = false

@description('Cognitive Services deployment configuration')
param deployments array = []

@description('Disable local authentication via API key')
param disableLocalAuth bool = false

@description('Cognitive Services account SKU')
param sku object = {
  name: 'S0'
}

@allowed([
  'CognitiveServices'
  'ComputerVision'
  'ContentModerator'
  'CustomVision.Training'
  'CustomVision.Prediction'
  'Face'
  'FormRecognizer'
  'ImmersiveReader'
  'LUIS'
  'Personalizer'
  'SpeechServices'
  'TextAnalytics'
  'TextTranslation'
  'AnomalyDetector'
  'OpenAI'
])
@description('The kind of Cognitive Services account to create')
param kind string

@description('Principal ID to assign roles to')
param principalId string = ''

@description('Type of principal (User, Group, ServicePrincipal)')
param principalType string = 'User'

@description('Virtual network name')
param virtualNetworkName string = ''

@description('Virtual network resource group name')
param vNetResourceGroupName string = ''

resource cognitiveServices 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: name
  location: location
  tags: tags
  kind: kind
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: customSubDomainName
    networkAcls: deployToVnet ? {
      defaultAction: 'Deny'
      virtualNetworkRules: [
        {
          id: resourceId(vNetResourceGroupName, 'Microsoft.Network/virtualNetworks/subnets', virtualNetworkName, 'cognitiveservices-subnet')
          ignoreMissingVnetServiceEndpoint: false
        }
      ]
      ipRules: []
    } : {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: deployToVnet ? 'Disabled' : 'Enabled'
    disableLocalAuth: disableLocalAuth
  }
  sku: sku
}

@batchSize(1)
resource cognitiveServicesDeployments 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = [for deployment in deployments: {
  parent: cognitiveServices
  name: deployment.name
  properties: {
    model: deployment.model
    raiPolicyName: contains(deployment, 'raiPolicyName') ? deployment.raiPolicyName : null
  }
  sku: contains(deployment, 'sku') ? deployment.sku : {
    name: 'Standard'
    capacity: deployment.capacity ?? 10
  }
}]

// Assign Cognitive Services OpenAI User role to the specified principal
resource cognitiveServicesOpenAIUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  scope: cognitiveServices
  name: guid(cognitiveServices.id, principalId, '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
    principalId: principalId
    principalType: principalType
  }
}

output endpoint string = cognitiveServices.properties.endpoint
output id string = cognitiveServices.id
output name string = cognitiveServices.name
