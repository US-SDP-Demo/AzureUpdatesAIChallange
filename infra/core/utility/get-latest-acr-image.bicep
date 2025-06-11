metadata description = 'Gets the latest image from Azure Container Registry.'

@description('Indicates if the container app already exists')
param exists bool

@description('The name of the container app')
param name string

resource existingApp 'Microsoft.App/containerApps@2023-05-01' existing = if (exists) {
  name: name
}

output containers array = exists ? existingApp.properties.template.containers : []
