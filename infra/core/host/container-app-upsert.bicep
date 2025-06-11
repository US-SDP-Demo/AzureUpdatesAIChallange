metadata description = 'Creates or updates a Container App.'

@description('The name of the Container App')
param name string

@description('Location for all resources')
param location string = resourceGroup().location

@description('Tags to apply to all resources')
param tags object = {}

@description('Allowed origins')
param allowedOrigins array = []

@description('Name of the container apps environment')
param containerAppsEnvironmentName string

@description('Container registry name')
param containerRegistryName string

@description('CPU cores allocated to a single container instance, e.g., 0.5')
param containerCpuCoreCount string = '0.5'

@description('The maximum number of replicas to run. Must be at least 1.')
@minValue(1)
param containerMaxReplicas int = 10

@description('Memory allocated to a single container instance, e.g., 1Gi')
param containerMemory string = '1.0Gi'

@description('The minimum number of replicas to run. Must be at least 0.')
@minValue(0)
param containerMinReplicas int = 1

@description('The name of the container')
param containerName string = 'main'

@description('The name of the container registry')
param containerRegistryHostname string = '${containerRegistryName}.azurecr.io'

@description('The environment variables for the container')
param env array = []

@description('Specifies if the resource exists')
param exists bool = false

@description('Specifies if Ingress is external')
param external bool = true

@description('The name of the user-assigned identity')
param identityName string = ''

@description('The type of identity for the resource')
@allowed(['None', 'SystemAssigned', 'UserAssigned'])
param identityType string = 'None'

@description('The image name')
param imageName string = ''

@description('Specifies if Ingress is enabled')
param ingressEnabled bool = true

@description('The secrets required for the container')
param secrets array = []

@description('The name of the container apps add-on service')
param serviceType string = ''

@description('The target port for the container')
param targetPort int = 80

module fetchLatestImage '../utility/get-latest-acr-image.bicep' = if (imageName == '') {
  name: '${name}-fetch-image'
  params: {
    exists: exists
    name: name
  }
}

resource app 'Microsoft.App/containerApps@2023-05-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: identityType
    userAssignedIdentities: identityType == 'UserAssigned' ? { '${resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', identityName)}': {} } : null
  }
  properties: {
    managedEnvironmentId: resourceId('Microsoft.App/managedEnvironments', containerAppsEnvironmentName)
    configuration: {
      activeRevisionsMode: 'single'
      ingress: ingressEnabled ? {
        external: external
        targetPort: targetPort
        transport: 'auto'
        corsPolicy: {
          allowedOrigins: union([ 'https://portal.azure.com', 'https://ms.portal.azure.com' ], allowedOrigins)
        }
      } : null
      registries: [
        {
          server: containerRegistryHostname
          identity: identityType == 'UserAssigned' ? resourceId('Microsoft.ManagedIdentity/userAssignedIdentities', identityName) : null
        }
      ]
      secrets: secrets
    }
    template: {
      containers: [        {
          image: imageName != '' ? imageName : fetchLatestImage.outputs.?containers[?0].?image ?? '${containerRegistryHostname}/simple-feed-reader:latest'
          name: containerName
          env: env
          resources: {
            cpu: json(containerCpuCoreCount)
            memory: containerMemory
          }
        }
      ]
      scale: {
        minReplicas: containerMinReplicas
        maxReplicas: containerMaxReplicas
      }
    }
  }
}

resource serviceConnectorAppService 'Microsoft.ServiceLinker/linkers@2022-05-01' = if (serviceType == 'redis') {
  name: 'redis'
  scope: app
  properties: {
    clientType: 'dotnet'
    targetService: {
      type: 'AzureResource'
      id: resourceId('Microsoft.Cache/Redis', serviceType)
    }
    authInfo: {
      authType: 'systemAssignedIdentity'
    }
  }
}

output defaultDomain string = app.properties.configuration.ingress.fqdn
output identityPrincipalId string = identityType == 'None' ? '' : (identityType == 'SystemAssigned' ? app.identity.principalId : '')
output imageName string = app.properties.template.containers[0].image
output name string = app.name
output uri string = ingressEnabled ? 'https://${app.properties.configuration.ingress.fqdn}' : ''
