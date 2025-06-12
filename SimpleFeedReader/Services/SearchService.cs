using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using SimpleFeedReader.ViewModels;
using Azure.Identity;

namespace SimpleFeedReader.Services
{
    public class SearchService
    {
        private readonly SearchClient _searchClient;
        private readonly SearchIndexClient _indexClient;
        private readonly ILogger<SearchService> _logger;
        private readonly string _indexName;
        public SearchService(IConfiguration configuration, ILogger<SearchService> logger)
        {
            _logger = logger;
            _indexName = configuration["AzureSearchIndexName"] ?? "rss-feeds";

            var searchEndpoint = configuration["AzureSearchEndpoint"];
            var searchApiKey = configuration["AzureSearchApiKey"];

            if (string.IsNullOrEmpty(searchEndpoint))
            {
                _logger.LogInformation("Azure Search endpoint is missing. Search functionality will be disabled.");
            }

            // Validate URI format before creating Uri objects
            if (!Uri.TryCreate(searchEndpoint, UriKind.Absolute, out var endpointUri))
            {
                _logger.LogWarning("Invalid Azure Search endpoint format: {Endpoint}. Search functionality will be disabled.", searchEndpoint);
            }

            // Use API key if available (for local development), otherwise use managed identity
            if (!string.IsNullOrEmpty(searchApiKey))
            {
                var credential = new AzureKeyCredential(searchApiKey);
                _indexClient = new SearchIndexClient(endpointUri, credential);
                _searchClient = new SearchClient(endpointUri, _indexName, credential);
                _logger.LogInformation("Initialized Azure Search with API key authentication");
            }
            else
            {
                var credential = new DefaultAzureCredential();
                _indexClient = new SearchIndexClient(endpointUri, credential);
                _searchClient = new SearchClient(endpointUri, _indexName, credential);
                _logger.LogInformation("Initialized Azure Search with managed identity authentication");
            }
        }
        public async Task<bool> InitializeIndexAsync()
        {
            if (_indexClient == null)
            {
                _logger.LogInformation("Azure Search is not configured. Skipping index initialization.");
                return false;
            }

            try
            {
                var indexDefinition = new SearchIndex(_indexName)
                {
                    Fields =
                    {
                        new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                        new SearchableField("title") { IsFilterable = true, IsSortable = true },
                        new SearchableField("content") { IsFilterable = true },
                        new SimpleField("uri", SearchFieldDataType.String) { IsFilterable = true },
                        new SimpleField("published", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                        new SimpleField("source", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                        new SearchableField("summary") { IsFilterable = true }
                    }
                };

                await _indexClient.CreateOrUpdateIndexAsync(indexDefinition);
                _logger.LogInformation($"Search index '{_indexName}' created or updated successfully.");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create or update search index '{_indexName}'.");
                return false;
            }
        }

        public async Task<bool> IndexNewsStoriesAsync(IEnumerable<NewsStoryViewModel> newsStories, string source = "rss-feed")
        {
            if (_searchClient == null || !newsStories.Any())
            {
                return false;
            }

            try
            {
                var searchDocuments = newsStories.Select(story => new SearchDocument
                {
                    ["id"] = GenerateDocumentId(story.Uri, story.Published),
                    ["title"] = story.Title ?? string.Empty,
                    ["content"] = story.Title ?? string.Empty, // For now, using title as content
                    ["uri"] = story.Uri ?? string.Empty,
                    ["published"] = story.Published,
                    ["source"] = source,
                    ["summary"] = GenerateSummary(story.Title)
                }).ToList();

                var batch = IndexDocumentsBatch.Upload(searchDocuments);
                var result = await _searchClient.IndexDocumentsAsync(batch);

                _logger.LogInformation($"Indexed {result.Value.Results.Count} news stories to search index.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to index news stories to search.");
                return false;
            }
        }

        public async Task<SearchResults<SearchDocument>> SearchNewsAsync(string query, int skip = 0, int take = 50)
        {
            if (_searchClient == null)
            {
                return null;
            }

            try
            {
                var searchOptions = new SearchOptions
                {
                    Skip = skip,
                    Size = take,
                    IncludeTotalCount = true,
                    OrderBy = { "published desc" }
                };

                searchOptions.Select.Add("id");
                searchOptions.Select.Add("title");
                searchOptions.Select.Add("uri");
                searchOptions.Select.Add("published");
                searchOptions.Select.Add("source");
                searchOptions.Select.Add("summary");

                var results = await _searchClient.SearchAsync<SearchDocument>(query, searchOptions);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to search for query: {query}");
                return null;
            }
        }

        public async Task<IEnumerable<NewsStoryViewModel>> GetLatestNewsAsync(int count = 10)
        {
            if (_searchClient == null)
            {
                return Enumerable.Empty<NewsStoryViewModel>();
            }

            try
            {
                var searchOptions = new SearchOptions
                {
                    Skip = 0,
                    Size = count,
                    OrderBy = { "published desc" }
                };

                searchOptions.Select.Add("title");
                searchOptions.Select.Add("uri");
                searchOptions.Select.Add("published");

                var results = await _searchClient.SearchAsync<SearchDocument>("*", searchOptions);
                
                return results.Value.GetResults().Select(result => new NewsStoryViewModel
                {
                    Summary = GenerateSummary(result.Document.GetString("title")),
                    FeedTitle = result.Document.GetString("source"),
                    Title = result.Document.GetString("title"),
                    Uri = result.Document.GetString("uri"),
                    Published = result.Document.GetDateTimeOffset("published") ?? DateTimeOffset.MinValue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get latest news from search index.");
                return Enumerable.Empty<NewsStoryViewModel>();
            }
        }
        
        private static string GenerateDocumentId(string uri, DateTimeOffset published)
        {
            // Create a unique ID based on URI and published date
            var combined = $"{uri}_{published:yyyyMMddHHmmss}";
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(combined))
                          .Replace('+', '-')
                          .Replace('/', '_')
                          .Replace("=", "");
        }

        private static string GenerateSummary(string title)
        {
            // Simple summary generation - in a real application, you might use AI to generate summaries
            if (string.IsNullOrEmpty(title) || title.Length <= 100)
            {
                return title ?? string.Empty;
            }

            return string.Concat(title.AsSpan(0, 97), "...");
        }
    }

    public class SearchDocument : Dictionary<string, object>
    {
        public string GetString(string key)
        {
            return TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        }

        public DateTimeOffset? GetDateTimeOffset(string key)
        {
            if (TryGetValue(key, out var value))
            {
                if (value is DateTimeOffset dto)
                    return dto;
                if (DateTimeOffset.TryParse(value?.ToString(), out var parsed))
                    return parsed;
            }
            return null;
        }
    }
}
