using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using SimpleFeedReader.Services;
using System.Reflection;

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
            var kernelBuilder = Kernel.CreateBuilder();

            kernelBuilder.Services.AddAzureOpenAIChatCompletion(
                deploymentName: Configuration["AzureOpenAIDeploymentName"],
                endpoint: Configuration["AzureOpenAIEndpoint"],
                apiKey: Configuration["AzureOpenAIApiKey"]
            );

            services.AddScoped<NewsService>();
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