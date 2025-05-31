using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SimpleFeedReader.Services;
using System.Reflection;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using System;
using System.IO;

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

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var kernelBuilder = Kernel.CreateBuilder();

            // Check if we have an API key or should use managed identity
            var openAiApiKey = configuration["AzureOpenAIApiKey"];
            if (!string.IsNullOrEmpty(openAiApiKey))
            {
                // Use API key for local development
                kernelBuilder.Services.AddAzureOpenAIChatCompletion(
                    deploymentName: configuration["AzureOpenAIDeploymentName"],
                    endpoint: configuration["AzureOpenAIEndpoint"],
                    apiKey: openAiApiKey
                );
            }
            else
            {
                // Use managed identity for Azure deployment
                var endpoint = configuration["AzureOpenAIEndpoint"];
                var deploymentName = configuration["AzureOpenAIDeploymentName"];
                
                if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(deploymentName))
                {
                    kernelBuilder.Services.AddAzureOpenAIChatCompletion(
                        deploymentName: deploymentName,
                        endpoint: endpoint,
                        new DefaultAzureCredential()
                    );
                }
            }

            services.AddScoped<NewsService>();
            services.AddScoped<SearchService>();
            services.AddHttpClient<ChatService>();
            services.AddScoped<ChatService>();
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            services.AddSingleton(kernelBuilder);
            services.AddRazorPages();
            services.AddControllers();
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
