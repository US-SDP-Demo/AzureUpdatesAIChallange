metadata description = 'Creates a container registry.'

@description('The name of the container registry')
param name string

@description('Location for the container registry')
param location string = resourceGroup().location

@description('Tags to apply to the container registry')
param tags object = {}

@description('Indicates whether admin user is enabled')
param adminUserEnabled bool = true

@description('Indicates whether anonymous pull is enabled')
param anonymousPullEnabled bool = false

@description('Indicates whether data endpoint is enabled')
param dataEndpointEnabled bool = false

@description('The encryption settings of the container registry')
param encryption object = {
  status: 'disabled'
}

@description('The network rule set for the container registry')
param networkRuleSet object = {
  defaultAction: 'Allow'
}

@description('The policies for the container registry')
param policies object = {
  quarantinePolicy: {
    status: 'disabled'
  }
  trustPolicy: {
    type: 'Notary'
    status: 'disabled'
  }
  retentionPolicy: {
    days: 7
    status: 'disabled'
  }
  exportPolicy: {
    status: 'enabled'
  }
  azureADAuthenticationAsArmPolicy: {
    status: 'enabled'
  }
  softDeletePolicy: {
    retentionDays: 7
    status: 'disabled'
  }
}

@description('Determines if registry used premium features')
param premium bool = false

@description('The container registry SKU')
param sku object = {
  name: premium ? 'Premium' : 'Basic'
}

@description('Indicates whether public network access is enabled')
param publicNetworkAccess string = 'Enabled'

@description('The zone redundancy setting')
param zoneRedundancy string = 'Disabled'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: sku
  properties: {    adminUserEnabled: adminUserEnabled
    anonymousPullEnabled: anonymousPullEnabled
    dataEndpointEnabled: dataEndpointEnabled
    encryption: encryption
    networkRuleSet: premium ? networkRuleSet : null
    policies: policies
    publicNetworkAccess: publicNetworkAccess
    zoneRedundancy: zoneRedundancy
  }
}

output loginServer string = containerRegistry.properties.loginServer
output name string = containerRegistry.name
