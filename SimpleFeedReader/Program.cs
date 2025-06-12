#pragma warning disable SKEXP0070 // Azure AI Inference is experimental

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureAIInference;
using SimpleFeedReader.Services;
using System.Reflection;
using Azure.Identity;

namespace SimpleFeedReader
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
            
            try
            {
                var options = new WebApplicationOptions
                {
                    ContentRootPath = Directory.GetCurrentDirectory(),
                    WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                    Args = args,
                    ApplicationName = "SimpleFeedReader"
                };
                
                var builder = WebApplication.CreateBuilder(options);
                Console.WriteLine("Minimal builder created successfully");
                
                // Force HTTP only in development
                if (builder.Environment.IsDevelopment())
                {
                    builder.WebHost.UseUrls("http://localhost:5000");
                }
                
                // Add user secrets in development
                if (builder.Environment.IsDevelopment())
                {
                    builder.Configuration.AddUserSecrets<Program>();
                }

                // Configure services
                ConfigureServices(builder.Services, builder.Configuration);

                var app = builder.Build();
                Console.WriteLine("Minimal app built successfully");
                
                // Configure the HTTP request pipeline
                Configure(app, app.Environment);

                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Type: {ex.GetType().FullName}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                Console.WriteLine($"Full Exception: {ex}");
                throw;
            }
        }
        private record InferenceOptions(
            [property: ConfigurationKeyName("AzureOpenAIDeploymentName")]
            string ModelId,
            [property: ConfigurationKeyName("AzureOpenAIEndpoint")]
            string Endpoint,
            [property: ConfigurationKeyName("AzureOpenAIApiKey")]
            string? ApiKey = null,
            [property: ConfigurationKeyName("GITHUB_TOKEN")]
            string? GithubToken = null);
        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            //TODO: Register vector db and related kernel memory services.
            var kernelBuilder = Kernel.CreateBuilder();
            services.AddScoped<NewsService>();
            services.AddScoped<SearchService>();
            services.AddHttpClient<ChatService>();
            services.AddScoped<ChatService>();
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            services.AddSingleton(kernelBuilder);
            services.AddRazorPages();
            services.AddControllers();
        }

        private const string AzureOpenAIApiKey = "AzureOpenAIApiKey";
        private const string GitHubToken = "GITHUB_TOKEN";
        private const string AzureOpenAIEndpoint = "AzureOpenAIEndpoint";
        private const string AzureOpenAIDeploymentName = "AzureOpenAIDeploymentName";

        public static void ConfigureKernelBuilder(IKernelBuilder kernelBuilder, IConfiguration configuration)
        {
            // Simplified AI service configuration - only branch on authentication method
            var apiKey = configuration[AzureOpenAIApiKey] ?? configuration[GitHubToken];
            var endpoint = configuration[AzureOpenAIEndpoint];
            var modelId = configuration[AzureOpenAIDeploymentName];
            
            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(modelId))
            {
                if (!string.IsNullOrEmpty(apiKey))
                {
                    // Use API key (either explicit or GitHub token)
                    kernelBuilder.AddAzureAIInferenceChatCompletion(
                        modelId: modelId,
                        apiKey: apiKey,
                        endpoint: new Uri(endpoint)
                    );
                }
                else
                {
                    // Use managed identity for Azure deployment
                    kernelBuilder.AddAzureAIInferenceChatCompletion(
                        modelId: modelId,
                        credential: new DefaultAzureCredential(),
                        endpoint: new Uri(endpoint)
                    );
                }
            }
        }

        private static void Configure(WebApplication app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            // Map controllers first (more specific routes)
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller}/{action=Index}/{id?}");
            
            // Then map Razor Pages (will handle the root route)
            app.MapRazorPages();
        }
    }
}
