using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using AutoMapper;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using Microsoft.Extensions.Logging;
using SimpleFeedReader.ViewModels;

namespace SimpleFeedReader.Services
{
    public class NewsService
    {
        private readonly IMapper _mapper;
        private readonly SearchService _searchService;
        private readonly ILogger<NewsService> _logger;

        public NewsService(IMapper mapper, SearchService searchService, ILogger<NewsService> logger)
        {
            _mapper = mapper;
            _searchService = searchService;
            _logger = logger;
        }

        public async Task<List<NewsStoryViewModel>> GetNews(string feedUrl)
        {
            var news = new List<NewsStoryViewModel>();
            var feedUri = new Uri(feedUrl);
            string feedTitle = "";

            using (var xmlReader = XmlReader.Create(feedUri.ToString(),
                   new XmlReaderSettings { Async = true }))
            {
                try
                {
                    var feedReader = new RssFeedReader(xmlReader);

                    while (await feedReader.Read())
                    {
                        switch (feedReader.ElementType)
                        {
                            // RSS Feed
                            case SyndicationElementType.Link:
                                ISyndicationLink link = await feedReader.ReadLink();
                                break;

                            // RSS Item
                            case SyndicationElementType.Item:
                                ISyndicationItem item = await feedReader.ReadItem();
                                var newsStory = _mapper.Map<NewsStoryViewModel>(item);
                                newsStory.FeedTitle = feedTitle;
                                news.Add(newsStory);
                                break;

                            // RSS Feed metadata
                            case SyndicationElementType.Content:
                                ISyndicationContent content = await feedReader.ReadContent();
                                if (string.IsNullOrEmpty(feedTitle) && !string.IsNullOrEmpty(content.Name) && content.Name.Equals("title", StringComparison.OrdinalIgnoreCase))
                                {
                                    feedTitle = content.Value ?? "";
                                }
                                break;

                            // Something else
                            default:
                                break;
                        }
                    }                    // Index the news stories in Azure Search (if SearchService is available)
                    if (news.Any() && _searchService != null)
                    {
                        var indexed = await _searchService.IndexNewsStoriesAsync(news, feedUrl);
                        if (indexed)
                        {
                            _logger.LogInformation($"Successfully indexed {news.Count} news stories from {feedUrl}");
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to index news stories from {feedUrl}");
                        }
                    }
                }
                catch (AggregateException ae)
                {
                    throw ae.Flatten();
                }
            }

            return news.OrderByDescending(story => story.Published).ToList();
        }
    }
    
    public class NewsStoryProfile : Profile
    {
        public NewsStoryProfile()
        {
            // Create the AutoMapper mapping profile between the 2 objects.
            // ISyndicationItem.Id maps to NewsStoryViewModel.Uri.
            CreateMap<ISyndicationItem, NewsStoryViewModel>()
                .ForMember(dest => dest.Uri, opts => opts.MapFrom(src => src.Id))
                .ForMember(dest => dest.Summary, opts => opts.MapFrom(src => src.Description ?? ""))
                .ForMember(dest => dest.FeedTitle, opts => opts.MapFrom(src => ""));
        }
    }
}
