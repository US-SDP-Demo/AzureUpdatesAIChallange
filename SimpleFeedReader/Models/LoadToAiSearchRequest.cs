using System.Collections.Generic;

namespace SimpleFeedReader.Models
{
    public class LoadToAiSearchRequest
    {
        public List<string> SelectedUrls { get; set; } = new List<string>();
    }
}
