using Microsoft.SemanticKernel;
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
            var inferenceOptions = configuration.Get<InferenceOptions>()
                ?? throw new InvalidOperationException("Inference options are not configured properly.");
            var apiKey = inferenceOptions.ApiKey ?? inferenceOptions.GithubToken;
            var modelId = inferenceOptions.ModelId;
            Uri endpoint = new(inferenceOptions.Endpoint);
            var kernelServices = kernelBuilder.Services;
            if (string.IsNullOrEmpty(apiKey))
            {
                kernelServices.AddAzureAIInferenceChatClient(
                    modelId: modelId,
                    endpoint: endpoint,
                    credential: new DefaultAzureCredential());
            }
            else
            {
                kernelServices.AddAzureAIInferenceChatClient(
                    modelId: modelId,
                    endpoint: endpoint,
                    apiKey: apiKey);
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
