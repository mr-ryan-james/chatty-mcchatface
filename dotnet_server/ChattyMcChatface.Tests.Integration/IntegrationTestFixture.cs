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
using ChattyMcChatface.Api.Services;
using ChattyMcChatface.Data.Entities;


namespace ChattyMcChatface.Tests.Integration
{
    public class IntegrationTestFixture : IDisposable
    {
        public IServiceProvider Services { get; }
        public IConfiguration Configuration { get; }
        private readonly ServiceProvider _serviceProvider;
        private static readonly object _dbLock = new object();
        private static bool _databaseInitialized = false;

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
            
            // Register SignalR services
            services.AddSignalR();
            
            // Add SQLite in-memory database for testing
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite("DataSource=TestDatabase;Mode=Memory;Cache=Shared");
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
            services.AddScoped<INotificationService, SignalRNotificationService>();
            
            // Build the service provider
            _serviceProvider = services.BuildServiceProvider();
            Services = _serviceProvider;
            
            // Initialize database only once across all fixture instances
            lock (_dbLock)
            {
                if (!_databaseInitialized)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    // Keep connection open for shared in-memory DB. Important!
                    dbContext.Database.OpenConnection();
                    // Apply migrations
                    dbContext.Database.Migrate();

                    // Seed basic data
                    var testUser = new User { Id = 1, FirstName = "Test", LastName = "User", Email = "test@example.com", PasswordHash = "hash" };
                    var testChatroom = new Chatroom { Id = 1, Title = "Integration Test Chatroom" };
                    // Add user first if Chatroom has FK constraint (or handle relationships appropriately)
                    if (!dbContext.Users.Any(u => u.Id == testUser.Id))
                    {
                        dbContext.Users.Add(testUser);
                    }
                    if (!dbContext.Chatrooms.Any(c => c.Id == testChatroom.Id))
                    {
                        dbContext.Chatrooms.Add(testChatroom);
                    }
                    dbContext.SaveChanges(); // Save seeded data

                    _databaseInitialized = true;
                }
            }
            Services = _serviceProvider; // Ensure Services is assigned after potential initialization
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}