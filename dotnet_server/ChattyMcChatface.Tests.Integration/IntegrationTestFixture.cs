using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ChattyMcChatface.Core.Services;
using ChattyMcChatface.Data;
using ChattyMcChatface.Data.Entities;
using System.Threading;
using ChattyMcChatface.Core.Services.AI.OpenAI;
using ChattyMcChatface.Core.Services.AI.Azure;
using ChattyMcChatface.Core.Services.AI.Claude;
using ChattyMcChatface.Core.Services.AI.Gemini;
using ChattyMcChatface.Core.Services.AI.Vertex;

namespace ChattyMcChatface.Tests.Integration
{
    public class IntegrationTestFixture : WebApplicationFactory<ChattyMcChatface.Api.Program>, IAsyncLifetime
    {
        private AppDbContext? _setupDbContext;

        public Mock<INotificationService> MockNotificationService { get; }

        public IntegrationTestFixture()
        {
            MockNotificationService = new Mock<INotificationService>();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });

            builder.ConfigureServices(services =>
            {
                // Replace AppDbContext with in-memory SQLite
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite("DataSource=TestDatabase;Mode=Memory;Cache=Shared");
                });
                services.AddHttpClient();
                services.AddSignalR();

                services.AddSingleton<IPersonaConfigService, PersonaConfigService>();
                services.AddScoped<IPersonaService, PersonaService>();

                services.AddSingleton<OpenAiModels>();
                services.AddSingleton<AzureAiModels>();
                services.AddSingleton<ClaudeModels>();
                services.AddSingleton<GeminiModels>();
                services.AddSingleton<VertexAiModels>();

                // Replace INotificationService with mock
                var notifDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(INotificationService));
                if (notifDescriptor != null)
                {
                    services.Remove(notifDescriptor);
                }

                services.AddSingleton(MockNotificationService);
                services.AddSingleton(sp => sp.GetRequiredService<Mock<INotificationService>>().Object);

                // Add test authentication
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        }

        public async Task InitializeAsync()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("DataSource=TestDatabase;Mode=Memory;Cache=Shared");
            // Ensure logging is configured if needed, e.g., optionsBuilder.UseLoggerFactory(...)

            _setupDbContext = new AppDbContext(optionsBuilder.Options);

            await _setupDbContext.Database.OpenConnectionAsync();
            await _setupDbContext.Database.MigrateAsync();
        }

        public new async Task DisposeAsync()
        {
            if (_setupDbContext != null)
            {
                await _setupDbContext.Database.CloseConnectionAsync();
                await _setupDbContext.DisposeAsync();
            }
            await base.DisposeAsync();
        }

        public HttpClient CreateClientWithAuth(string userId = "1", string userName = "TestUser")
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            return client;
        }

        public async Task ResetDatabaseAsync(IServiceScopeFactory scopeFactory)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM ChatMessages");
            await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM LastReads");
            await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Chatrooms");
            await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Users");
        }

        public async Task<User> SeedUserAsync(string firstName, string lastName, string email, bool isPersona, IServiceScopeFactory scopeFactory, int? id = null)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                IsPersona = isPersona,
                PasswordHash = "hash"
            };

            if (id.HasValue)
            {
                user.Id = id.Value;
            }

            // Check if user with this ID already exists
            if (id.HasValue)
            {
                var existingUser = await dbContext.Users.FindAsync(id.Value);
                if (existingUser != null)
                {
                    // Update existing user properties
                    existingUser.FirstName = firstName;
                    existingUser.LastName = lastName;
                    existingUser.Email = email;
                    existingUser.IsPersona = isPersona;
                    await dbContext.SaveChangesAsync();
                    return existingUser;
                }
            }
            
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            return user;
        }

        public async Task<Chatroom> SeedChatroomAsync(List<User> users, int? personaUserId, string title, IServiceScopeFactory scopeFactory)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var chatroom = new Chatroom { Title = title, PersonaUserId = personaUserId };

            foreach (var user in users)
            {
                // For each user in the list, check if it's already in the database
                var existingUser = await dbContext.Users.FindAsync(user.Id);
                if (existingUser != null)
                {
                    // If it exists, add the existing user to the chatroom
                    chatroom.Users.Add(existingUser);
                }
                else
                {
                    // If it doesn't exist, add the original user
                    chatroom.Users.Add(user);
                }
            }

            dbContext.Chatrooms.Add(chatroom);
            await dbContext.SaveChangesAsync();
            return chatroom;
        }

        public async Task SeedMessagesAsync(int chatroomId, int count, DateTime startDate, IServiceScopeFactory scopeFactory)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var chatroom = await dbContext.Chatrooms.FindAsync(chatroomId);
            if (chatroom == null) throw new Exception($"Chatroom with ID {chatroomId} not found for seeding messages.");

            var user1 = await dbContext.Users.FindAsync(1);
            var user1001 = await dbContext.Users.FindAsync(1001);
            if (user1 == null || user1001 == null) throw new Exception("Required users (1 or 1001) not found for seeding messages.");

            var messages = new List<ChatMessage>();
            for (int i = 0; i < count; i++)
            {
                var userId = (i % 2 == 0) ? 1 : 1001;
                messages.Add(new ChatMessage
                {
                    ChatroomId = chatroomId,
                    UserId = userId,
                    Text = $"Message {i + 1}",
                    Date = startDate.AddMinutes(i),
                    Chatroom = chatroom,
                    User = (userId == 1) ? user1 : user1001
                });
            }
            await dbContext.ChatMessages.AddRangeAsync(messages);
            await dbContext.SaveChangesAsync();
        }

        public async Task SeedLastReadAsync(int chatroomId, int userId, DateTime lastReadDate, IServiceScopeFactory scopeFactory)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
            var user = await dbContext.Users.FindAsync(userId);
            var chatroom = await dbContext.Chatrooms.FindAsync(chatroomId);
            if (user == null) throw new Exception($"User with ID {userId} not found");
            if (chatroom == null) throw new Exception($"Chatroom with ID {chatroomId} not found");
    
            var lastRead = new LastRead
            {
                ChatroomId = chatroomId,
                UserId = userId,
                LastReadDate = lastReadDate,
                User = user,
                Chatroom = chatroom
            };
    
            dbContext.LastReads.Add(lastRead);
            await dbContext.SaveChangesAsync();
        }
    }
}