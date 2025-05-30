# Simple Feed Reader

A modern RSS feed reader application built with .NET 9.0, featuring Azure Search integration and AI-powered chat capabilities.

## Features

- **RSS Feed Reading**: Retrieve and display RSS feeds from any valid feed URL
- **Azure Search Integration**: Automatically index RSS feed content for fast, full-text search
- **AI Chat**: Powered by Azure OpenAI for intelligent conversations
- **Modern UI**: Clean, responsive interface built with Bootstrap and Tailwind CSS
- **Cloud-Ready**: Designed for deployment to Azure Container Apps

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Azure Developer CLI (azd)](https://aka.ms/azd-install)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for containerization)
- Azure subscription

## Quick Start

### Local Development

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd simple-feed-reader
   ```

2. Configure Azure services (optional for local development):
   ```bash
   # Copy appsettings template
   cp SimpleFeedReader/appsettings.json SimpleFeedReader/appsettings.Development.json
   
   # Edit appsettings.Development.json and add your Azure service endpoints:
   # - AzureOpenAI section (Endpoint, ApiKey, DeploymentName)
   # - AzureSearchEndpoint, AzureSearchApiKey, AzureSearchIndexName
   ```

3. Run the application:
   ```bash
   dotnet run --project SimpleFeedReader
   ```

4. Open your browser to `https://localhost:5001`

### Azure Deployment with azd

This application includes complete Azure Developer CLI (azd) configuration for one-click deployment to Azure.

1. **Initialize the deployment**:
   ```bash
   azd init
   ```

2. **Deploy to Azure**:
   ```bash
   azd up
   ```

   This will:
   - Create Azure resources (Container Apps, Azure Search, Azure OpenAI, etc.)
   - Build and deploy the application
   - Configure all necessary service connections

3. **Access your deployed application**:
   ```bash
   azd show
   ```

## Architecture

### Azure Resources

The application deploys the following Azure resources:

- **Azure Container Apps**: Hosts the web application
- **Azure Cognitive Search**: Provides full-text search capabilities for RSS content
- **Azure OpenAI**: Powers the AI chat functionality
- **Azure Container Registry**: Stores the application container image
- **Azure Log Analytics**: Application monitoring and logging
- **Managed Identity**: Secure service-to-service authentication

### Application Components

- **SimpleFeedReader**: Main web application (.NET 9.0 Razor Pages)
- **NewsService**: RSS feed processing and management
- **SearchService**: Azure Search integration for indexing and searching
- **ChatService**: Azure OpenAI integration for AI conversations

## Usage

### RSS Feed Reading

1. Navigate to the home page
2. Enter any valid RSS feed URL (e.g., `https://feeds.bbci.co.uk/news/rss.xml`)
3. Click "Retrieve Feed" to load and display articles
4. Articles are automatically indexed in Azure Search for future searching

### Search Functionality

1. Use the search box on the home page
2. Enter keywords to search across all indexed RSS content
3. Results show title, description, publish date, and source feed
4. Search works across all previously loaded RSS feeds

### AI Chat

1. Navigate to the `/Chat` page
2. Ask questions about technology, news, or general topics
3. The AI assistant provides helpful responses using Azure OpenAI

## Configuration

### Application Settings

Key configuration settings in `appsettings.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-openai-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4"
  },
  "AzureSearchEndpoint": "https://your-search-service.search.windows.net",
  "AzureSearchApiKey": "your-search-api-key",
  "AzureSearchIndexName": "news-articles"
}
```

### Environment Variables

For production deployment, use environment variables or Azure App Configuration:

- `AzureOpenAI__Endpoint`
- `AzureOpenAI__ApiKey`
- `AzureOpenAI__DeploymentName`
- `AzureSearchEndpoint`
- `AzureSearchApiKey`
- `AzureSearchIndexName`

## Development

### Project Structure

```
SimpleFeedReader/
├── Controllers/          # API controllers
├── Pages/               # Razor pages
├── Services/            # Business logic services
├── ViewModels/          # Data transfer objects
├── Views/               # MVC views
└── wwwroot/            # Static assets

SimpleFeedReader.Tests/  # Unit tests
infra/                   # Azure infrastructure (Bicep)
```

### Running Tests

```bash
dotnet test
```

### Building for Production

```bash
dotnet publish -c Release
```

## Contributing

Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on contributing to this project.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.