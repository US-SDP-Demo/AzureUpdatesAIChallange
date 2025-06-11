metadata description = 'Creates a user-assigned managed identity.'

@description('The name of the managed identity')
param name string

@description('Location for the managed identity')
param location string = resourceGroup().location

@description('Tags to apply to the managed identity')
param tags object = {}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: name
  location: location
  tags: tags
}

output clientId string = managedIdentity.properties.clientId
output id string = managedIdentity.id
output name string = managedIdentity.name
output principalId string = managedIdentity.properties.principalId
