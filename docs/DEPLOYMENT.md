# Azure Deployment Guide

This guide walks you through deploying the Simple Feed Reader application to Azure using the Azure Developer CLI (azd).

## Prerequisites

1. **Azure Developer CLI**: Install from https://aka.ms/azd-install
2. **Azure CLI**: Install from https://docs.microsoft.com/cli/azure/install-azure-cli
3. **Docker Desktop**: Required for building container images
4. **Azure Subscription**: With appropriate permissions to create resources

## Deployment Steps

### 1. Initialize the Project

```bash
# Clone the repository
git clone <repository-url>
cd simple-feed-reader

# Initialize azd (if not already done)
azd init
```

### 2. Configure Environment

```bash
# Create a new environment (replace 'dev' with your preferred name)
azd env new dev

# Set the Azure location (optional, defaults to eastus2)
azd env set AZURE_LOCATION eastus2
```

### 3. Deploy to Azure

```bash
# Deploy all resources and application
azd up
```

This command will:
- Create a new resource group
- Deploy Azure infrastructure using Bicep templates
- Build the application container
- Push the container to Azure Container Registry
- Deploy the container to Azure Container Apps
- Configure service connections and managed identities

### 4. Verify Deployment

```bash
# Get the application URL
azd show

# View deployment status
azd show --output table
```

## Created Azure Resources

The deployment creates the following resources:

### Core Application Resources
- **Resource Group**: Contains all resources for the environment
- **Azure Container Apps Environment**: Hosting platform for containers
- **Azure Container Apps**: Hosts the web application
- **Azure Container Registry**: Stores the application container image

### AI and Search Services
- **Azure Cognitive Search**: Basic SKU search service for RSS content indexing
- **Azure OpenAI**: For AI chat functionality (gpt-4 model)

### Security and Monitoring
- **Managed Identity**: For secure service-to-service authentication
- **Log Analytics Workspace**: Application monitoring and logging
- **Application Insights**: Application performance monitoring

### Networking and Access Control
- **Role Assignments**: Proper RBAC configuration for managed identities
- **Service connections**: Secure configuration between services

## Resource Naming Convention

Resources follow this naming pattern:
- Resource Group: `rg-{environment-name}`
- Container Apps: `ca-{environment-name}`
- Search Service: `srch-{environment-name}`
- OpenAI Service: `oai-{environment-name}`
- Container Registry: `cr{environment-name}{unique-suffix}`

## Configuration Details

### Environment Variables

The application is configured with these environment variables:

```bash
# Automatically set by azd deployment
AZURE_OPENAI_ENDPOINT=<deployed-openai-endpoint>
AZURE_SEARCH_ENDPOINT=<deployed-search-endpoint>
AZURE_SEARCH_INDEX_NAME=news-articles

# Authentication via Managed Identity (recommended)
# Or use API keys for development:
AZURE_OPENAI_API_KEY=<your-api-key>
AZURE_SEARCH_API_KEY=<your-api-key>
```

### Managed Identity Configuration

The deployment configures managed identities with appropriate roles:
- **Cognitive Services OpenAI User**: For Azure OpenAI access
- **Search Index Data Contributor**: For Azure Search operations
- **Search Service Contributor**: For search service management

## Post-Deployment Steps

### 1. Verify Services

```bash
# Test the application
curl https://your-app-url.azurecontainerapps.io

# Check container logs
azd monitor --live
```

### 2. Initialize Search Index

The search index is automatically created when the application starts. To manually trigger index creation:

1. Navigate to your deployed application
2. Load any RSS feed to trigger index initialization
3. Use the search functionality to verify it's working

### 3. Configure AI Model (if needed)

If you need to deploy additional AI models:

```bash
# Connect to Azure
az login

# List available models
az cognitiveservices account list-models --name <your-openai-service>

# Deploy additional models if needed
az cognitiveservices account deployment create \
  --resource-group <your-resource-group> \
  --name <your-openai-service> \
  --deployment-name gpt-35-turbo \
  --model-name gpt-35-turbo \
  --model-version "0301" \
  --model-format OpenAI \
  --sku-capacity 10 \
  --sku-name Standard
```

## Troubleshooting

### Common Issues

1. **Build Failures**
   ```bash
   # Check Docker is running
   docker info
   
   # Rebuild and redeploy
   azd deploy
   ```

2. **Resource Creation Errors**
   ```bash
   # Check subscription permissions
   az account show
   
   # Verify resource provider registration
   az provider register --namespace Microsoft.App
   az provider register --namespace Microsoft.Search
   az provider register --namespace Microsoft.CognitiveServices
   ```

3. **Application Not Starting**
   ```bash
   # Check container logs
   azd monitor --live
   
   # View application insights
   az monitor app-insights query \
     --app <your-app-insights> \
     --analytics-query "traces | order by timestamp desc | take 50"
   ```

### Debugging Commands

```bash
# View all resources
azd show --output table

# Get application logs
azd monitor

# Check deployment status
az deployment group list --resource-group <your-rg>

# Test connectivity
az container app show --name <your-app-name> --resource-group <your-rg>
```

## Updating the Application

```bash
# Deploy code changes
azd deploy

# Update infrastructure
azd provision

# Full update (infrastructure + application)
azd up
```

## Cleanup

```bash
# Remove all Azure resources
azd down

# Delete the environment
azd env delete <environment-name>
```

## Cost Considerations

Estimated monthly costs (USD, as of 2024):
- Azure Container Apps: ~$30-50
- Azure Cognitive Search (Basic): ~$250
- Azure OpenAI (pay-per-use): Variable based on usage
- Azure Container Registry: ~$5
- Log Analytics: ~$10-20

Total estimated cost: ~$300-400/month

To reduce costs:
- Use shared Azure OpenAI resources
- Consider free tier services for development
- Monitor usage through Azure Cost Management
