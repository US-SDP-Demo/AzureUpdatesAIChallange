using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFeedReader.Controllers
{
    public class HomeController : Controller
    {
        [HttpPost]
        [Route("/Home/LoadToAiSearch")]
        public async Task<IActionResult> LoadToAiSearch([FromBody] LoadToAiSearchRequest request)
        {
            try
            {
                if (request?.SelectedUrls == null || !request.SelectedUrls.Any())
                {
                    return Json(new { success = false, message = "No articles selected." });
                }

                // TODO: Implement your AI Search loading logic here
                // For example:
                // - Fetch article content from the URLs
                // - Process and index the content in Azure AI Search
                // - Store references in your database
                
                // Simulate processing time
                await Task.Delay(1000);
                
                // Log the selected URLs for debugging
                foreach (var url in request.SelectedUrls)
                {
                    Console.WriteLine($"Processing article: {url}");
                }

                return Json(new { 
                    success = true, 
                    message = $"Successfully processed {request.SelectedUrls.Count} articles.",
                    processedCount = request.SelectedUrls.Count
                });
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error loading articles to AI Search: {ex.Message}");
                
                return Json(new { 
                    success = false, 
                    message = "An error occurred while processing the articles." 
                });
            }
        }
    }

    public class LoadToAiSearchRequest
    {
        public List<string> SelectedUrls { get; set; } = new List<string>();
    }
}
