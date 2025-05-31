using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SimpleFeedReader.Services;
using SimpleFeedReader.ViewModels;

namespace SimpleFeedReader.Pages
{
    public class IndexModel : PageModel
    {
        private readonly NewsService _newsService;
        private readonly SearchService _searchService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(NewsService newsService, SearchService searchService, ILogger<IndexModel> logger)
        {
            _newsService = newsService;
            _searchService = searchService;
            _logger = logger;
        }

        public string ErrorText { get; private set; }

        public List<NewsStoryViewModel> NewsItems { get; private set; }        public async Task OnGet()
        {
            string feedUrl = Request.Query["feedurl"];
            string searchQuery = Request.Query["search"];

            // Only initialize search if Azure Search is configured and there's a search query
            if (!string.IsNullOrEmpty(searchQuery) && _searchService != null)
            {
                try
                {
                    // Initialize the search index only when needed
                    var isInitialized = await _searchService.InitializeIndexAsync();
                    if (isInitialized)
                    {
                        var searchResults = await _searchService.SearchNewsAsync(searchQuery);
                        if (searchResults != null)
                        {
                            NewsItems = new List<NewsStoryViewModel>();
                            foreach (var result in searchResults.GetResults())
                            {
                                NewsItems.Add(new NewsStoryViewModel
                                {                                    Title = result.Document.GetString("title") ?? "",
                                    Uri = result.Document.GetString("uri") ?? "",
                                    Published = result.Document.GetDateTimeOffset("published") ?? DateTimeOffset.MinValue,
                                Summary = result.Document.GetString("summary") ?? "",                                    FeedTitle = result.Document.GetString("feedTitle") ?? ""
                                });
                            }
                        }
                        _logger.LogInformation($"Search for '{searchQuery}' returned {NewsItems.Count} results.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Search failed for query: {searchQuery}");
                    ErrorText = "Search is currently unavailable. Please try again later.";
                }
            }
            // If there's a feed URL, fetch and index the news
            else if (!string.IsNullOrEmpty(feedUrl))
            {
                try
                {
                    NewsItems = await _newsService.GetNews(feedUrl);
                }
                catch (UriFormatException)
                {
                    ErrorText = "There was a problem parsing the URL.";
                    return;
                }
                catch (WebException ex) when (ex.Status == WebExceptionStatus.NameResolutionFailure)
                {
                    ErrorText = "Unknown host name.";
                    return;
                }
                catch (WebException ex) when (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    ErrorText = "Syndication feed not found.";
                    return;
                }
                catch (AggregateException ae)
                {
                    ae.Handle((x) =>
                    {
                        if (x is XmlException)
                        {
                            ErrorText = "There was a problem parsing the feed. Are you sure that URL is a syndication feed?";
                            return true;
                        }
                        return false;
                    });
                }
            }
            else
            {
                // If no feed URL or search query, show latest news from search index
                try
                {
                    var latestNews = await _searchService.GetLatestNewsAsync(20);
                    NewsItems = latestNews.ToList();
                    if (NewsItems.Any())
                    {
                        _logger.LogInformation($"Loaded {NewsItems.Count} latest news stories from search index.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load latest news from search index.");
                }
            }
        }
    }
}
