using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SimpleFeedReader.Services
{
    public class ChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ChatService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> SendMessageAsync(string message)
        {
            try
            {
                // For now, return a simple mock response
                // In a real implementation, you would call an AI service like OpenAI, Azure OpenAI, etc.
                await Task.Delay(1000); // Simulate API call delay
                
                var responses = new[]
                {
                    "That's an interesting question! Let me think about that...",
                    "I can help you with that. Here's what I think...",
                    "Thanks for asking! Based on my understanding...",
                    "Great question! Here's my perspective...",
                    "I'd be happy to help you with that topic."
                };

                var random = new Random();
                var response = responses[random.Next(responses.Length)];
                
                return $"{response} You asked: '{message}'";
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending message to AI service: {ex.Message}");
            }
        }
    }
}