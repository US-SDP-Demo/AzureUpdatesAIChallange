metadata description = 'Creates a role assignment.'

@description('The ID of the principal to assign the role to')
param principalId string

@description('The ID of the role definition to assign')
param roleDefinitionId string

@description('The type of principal (User, Group, ServicePrincipal)')
param principalType string = 'ServicePrincipal'

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, principalId, roleDefinitionId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionId)
    principalId: principalId
    principalType: principalType
  }
}

output roleAssignmentId string = roleAssignment.id
