metadata description = 'Creates secrets for container app from Azure service keys.'

@description('The name of the OpenAI service to get keys from')
param openAiServiceName string

@description('The name of the Search service to get keys from')
param searchServiceName string

resource openaiService 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = {
  name: openAiServiceName
}

resource searchService 'Microsoft.Search/searchServices@2015-08-19' existing = {
  name: searchServiceName
}

output openAiKey string = openaiService.listKeys().key1
output searchKey string = searchService.listKeys().primaryKey
