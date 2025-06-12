namespace SimpleFeedReader.ViewModels
{
    public class NewsStoryViewModel
    {
        public DateTimeOffset Published { get; set; }

        public required string Title { get; set; }

        public required string Uri { get; set; }

        public required string Summary { get; set; }

        public required string FeedTitle { get; set; }
    }
}
