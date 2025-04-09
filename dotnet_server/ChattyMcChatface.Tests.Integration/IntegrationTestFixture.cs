using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ChattyMcChatface.Core.Services;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Data;
using System;
using System.Net.Http;
using ChattyMcChatface.Core.Services.AI.OpenAI;
using ChattyMcChatface.Core.Services.AI.Azure;
using ChattyMcChatface.Core.Services.AI.Claude;
using ChattyMcChatface.Core.Services.AI.Gemini;
using ChattyMcChatface.Core.Services.AI.Vertex;

namespace ChattyMcChatface.Tests.Integration
{
    public class IntegrationTestFixture : IDisposable
    {
        public IServiceProvider Services { get; }
        public IConfiguration Configuration { get; }
        private readonly ServiceProvider _serviceProvider;

        public IntegrationTestFixture()
        {
            // Build configuration with user secrets and environment variables
            Configuration = new ConfigurationBuilder()
                .AddUserSecrets<IntegrationTestFixture>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Set up service collection
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(configure => configure.AddConsole());
            
            // Add configuration
            services.AddSingleton(Configuration);
            
            // Add SQLite in-memory database for testing
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite("DataSource=:memory:");
            });
            
            // Add HTTP client factory
            services.AddHttpClient();
            services.AddHttpClient("GeminiApi", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            
            // Register AI services
            services.AddScoped<OpenAiProvider>();
            services.AddScoped<AzureAiProvider>();
            services.AddScoped<GeminiProvider>();
            services.AddScoped<ClaudeProvider>();
            services.AddScoped<VertexAiProvider>();

            // Register model classes
            services.AddSingleton<OpenAiModels>();
            services.AddSingleton<AzureAiModels>();
            services.AddSingleton<ClaudeModels>();
            services.AddSingleton<GeminiModels>();
            services.AddSingleton<VertexAiModels>();

            // Register persona services
            services.AddSingleton<IPersonaConfigService, PersonaConfigService>();
            services.AddScoped<IPersonaService, PersonaService>();
            
            // Build the service provider
            _serviceProvider = services.BuildServiceProvider();
            Services = _serviceProvider;
            
            // Initialize database if needed
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.OpenConnection();
            dbContext.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}