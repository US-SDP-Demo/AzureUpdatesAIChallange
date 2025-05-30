using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using SimpleFeedReader.Services;
using System.Reflection;
using Azure.Identity;

namespace SimpleFeedReader
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        public void ConfigureServices(IServiceCollection services)
        {
            var kernelBuilder = Kernel.CreateBuilder();            // Check if we have an API key or should use managed identity
            var openAiApiKey = Configuration["AzureOpenAIApiKey"];
            if (!string.IsNullOrEmpty(openAiApiKey))
            {
                // Use API key for local development
                kernelBuilder.Services.AddAzureOpenAIChatCompletion(
                    deploymentName: Configuration["AzureOpenAIDeploymentName"],
                    endpoint: Configuration["AzureOpenAIEndpoint"],
                    apiKey: openAiApiKey
                );
            }
            else
            {
                // Use managed identity for Azure deployment
                kernelBuilder.Services.AddAzureOpenAIChatCompletion(
                    deploymentName: Configuration["AzureOpenAIDeploymentName"],
                    endpoint: Configuration["AzureOpenAIEndpoint"],
                    new DefaultAzureCredential()
                );
            }

            services.AddScoped<NewsService>();
            services.AddScoped<SearchService>();
            services.AddHttpClient<ChatService>();
            services.AddScoped<ChatService>();
            services.AddAutoMapper(Assembly.GetExecutingAssembly());
            services.AddSingleton(kernelBuilder);
            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}