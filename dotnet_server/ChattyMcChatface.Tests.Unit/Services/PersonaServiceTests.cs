using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Services.AI.Azure;
using ChattyMcChatface.Core.Services.AI.Claude;
using ChattyMcChatface.Core.Services.AI.Gemini;
using ChattyMcChatface.Core.Services.AI.OpenAI;
using ChattyMcChatface.Core.Services.AI.Vertex;
using ChattyMcChatface.Data;
using ChattyMcChatface.Data.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; // Added this line
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace ChattyMcChatface.Tests.Unit.Services
{
    public class PersonaServiceTests
    {
        // --- Existing Test ---
        [Fact]
        public async Task MockQueryable_SupportsLinqOperations_ForDbContextMocking()
        {
            // This test demonstrates that MockQueryable correctly supports LINQ for DbSet mocking
            
            // Arrange - Create test entities with all required properties
            var regularUser = new User 
            { 
                Id = 1, 
                FirstName = "Regular", 
                LastName = "User", 
                Email = "user@example.com", 
                PasswordHash = "hashedpassword123" 
            };
            
            var aiUser = new User 
            { 
                Id = 999, 
                FirstName = "AI", 
                LastName = "Assistant", 
                Email = "ai@example.com", 
                PasswordHash = "aihashedpassword", 
                IsPersona = true 
            };
            
            var users = new List<User> { regularUser, aiUser };
            
            // Create chatroom
            var chatroom = new Chatroom 
            { 
                Id = 1, 
                Title = "Test Chatroom", 
                PersonaUserId = 999,
                PersonaUser = aiUser
            };
            chatroom.Users.Add(regularUser);
            chatroom.Users.Add(aiUser);
            
            var chatrooms = new List<Chatroom> { chatroom };
            
            // ChatMessages need both User and Chatroom
            var message = new ChatMessage 
            { 
                Id = 1, 
                ChatroomId = 1, 
                UserId = 1, 
                Text = "Hello AI!", 
                Date = DateTime.UtcNow.AddMinutes(-5),
                User = regularUser,
                Chatroom = chatroom
            };
            
            var chatMessages = new List<ChatMessage> { message };
            
            // Update references
            chatroom.Chats.Add(message);
            regularUser.Messages.Add(message);
            
            // Build mock DbSets using MockQueryable
            var usersMockDbSet = users.AsQueryable().BuildMockDbSet();
            var chatroomsMockDbSet = chatrooms.AsQueryable().BuildMockDbSet();
            var chatMessagesMockDbSet = chatMessages.AsQueryable().BuildMockDbSet();
            
            // Setup the mock DbContext
            var mockDbContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            mockDbContext.Setup(c => c.Users).Returns(usersMockDbSet.Object);
            mockDbContext.Setup(c => c.Chatrooms).Returns(chatroomsMockDbSet.Object);
            mockDbContext.Setup(c => c.ChatMessages).Returns(chatMessagesMockDbSet.Object);
            
            // Setup AddAsync for ChatMessages without trying to return EntityEntry
            chatMessagesMockDbSet.Setup(d => d.AddAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ChatMessage, CancellationToken>((msg, _) => {
                    // Ensure the new message has the required navigation properties
                    var user = users.FirstOrDefault(u => u.Id == msg.UserId);
                    var room = chatrooms.FirstOrDefault(c => c.Id == msg.ChatroomId);
                    if (user != null) msg.User = user;
                    if (room != null) msg.Chatroom = room;
                    
                    chatMessages.Add(msg);
                    
                    // Update the collections
                    if (user != null) user.Messages.Add(msg);
                    if (room != null) room.Chats.Add(msg);
                })
                // Return default EntityEntry as it's not used and mocking is problematic
                .Returns((ChatMessage entity, CancellationToken _) => ValueTask.FromResult<EntityEntry<ChatMessage>>(default!));
            
            // Setup SaveChangesAsync
            mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            
            // Act: Test LINQ queries
            var recentMessages = await mockDbContext.Object.ChatMessages
                .Where(m => m.ChatroomId == 1)
                .OrderByDescending(m => m.Date)
                .Take(20)
                .ToListAsync();
            
            var specificChatroom = await mockDbContext.Object.Chatrooms
                .FirstOrDefaultAsync(c => c.Id == 1);
            
            var aiUserFromDb = await mockDbContext.Object.Users
                .FirstOrDefaultAsync(u => u.Id == 999);
            
            // Assert
            recentMessages.Should().NotBeEmpty();
            recentMessages.Should().HaveCount(1);
            specificChatroom.Should().NotBeNull();
            specificChatroom?.PersonaUserId.Should().Be(999);
            aiUserFromDb.Should().NotBeNull();
            aiUserFromDb?.FirstName.Should().Be("AI");
            aiUserFromDb?.LastName.Should().Be("Assistant");
            
            // Test adding a new message
            var newMessage = new ChatMessage
            {
                Id = 2,
                ChatroomId = 1,
                UserId = 999,
                Text = "I am an AI assistant.",
                Date = DateTime.UtcNow,
                User = aiUser,
                Chatroom = chatroom
            };
            
            await chatMessagesMockDbSet.Object.AddAsync(newMessage);
            await mockDbContext.Object.SaveChangesAsync();
            
            // Verify the message was added
            chatMessages.Should().HaveCount(2);
            chatMessages.Should().Contain(newMessage);
            
            // Test we can query for it
            var allMessages = await mockDbContext.Object.ChatMessages
                .Where(m => m.ChatroomId == 1)
                .OrderBy(m => m.Date)
                .ToListAsync();
            
            allMessages.Should().HaveCount(2);
            allMessages[1].Text.Should().Be("I am an AI assistant.");
            allMessages[1].UserId.Should().Be(999);
        }

        // --- New Azure Test ---
        [Fact]
        public async Task GenerateResponseAsync_WhenAzureGpt4oIsPreferred_CallsAzureGpt4oThrivify()
        {
            // Arrange
            const int chatroomId = 1;
            const int personaUserId = 999;
            const string expectedResponse = "Mocked Azure GPT-4o Response";
            const string systemPrompt = "You are a helpful assistant.";

            var regularUser = new User { Id = 1, FirstName = "Regular", LastName = "User", Email = "user@example.com", PasswordHash = "hash" };
            var aiUser = new User { Id = personaUserId, FirstName = "AI", LastName = "Assistant", Email = "ai@example.com", PasswordHash = "aihash", IsPersona = true };
            var users = new List<User> { regularUser, aiUser };

            var chatroom = new Chatroom { Id = chatroomId, Title = "Azure Test", PersonaUserId = personaUserId, PersonaUser = aiUser };
            chatroom.Users.Add(regularUser);
            chatroom.Users.Add(aiUser);
            var chatrooms = new List<Chatroom> { chatroom };

            var triggeringMessageEntity = new ChatMessage
            {
                Id = 1, ChatroomId = chatroomId, UserId = 1, Text = "Hello Azure!", Date = DateTime.UtcNow.AddMinutes(-1), User = regularUser, Chatroom = chatroom
            };
            var chatMessages = new List<ChatMessage> { triggeringMessageEntity };
            chatroom.Chats.Add(triggeringMessageEntity);
            regularUser.Messages.Add(triggeringMessageEntity);

            var usersMockDbSet = users.AsQueryable().BuildMockDbSet();
            var chatroomsMockDbSet = chatrooms.AsQueryable().BuildMockDbSet();
            var chatMessagesMockDbSet = chatMessages.AsQueryable().BuildMockDbSet();

            var mockDbContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            mockDbContext.Setup(c => c.Users).Returns(usersMockDbSet.Object);
            mockDbContext.Setup(c => c.Chatrooms).Returns(chatroomsMockDbSet.Object);
            mockDbContext.Setup(c => c.ChatMessages).Returns(chatMessagesMockDbSet.Object);
            ChatMessage? addedMessage = null;
            chatMessagesMockDbSet.Setup(d => d.AddAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ChatMessage, CancellationToken>((msg, _) => {
                    msg.Id = chatMessages.Count + 1; // Simulate ID generation
                    msg.User = users.First(u => u.Id == msg.UserId);
                    msg.Chatroom = chatrooms.First(c => c.Id == msg.ChatroomId);
                    chatMessages.Add(msg);
                    addedMessage = msg;
                })
                // Return default EntityEntry as it's not used and mocking is problematic
                .Returns((ChatMessage entity, CancellationToken _) => ValueTask.FromResult<EntityEntry<ChatMessage>>(default!));

            mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var mockPersonaConfigService = new Mock<IPersonaConfigService>();
            mockPersonaConfigService.Setup(s => s.GetConfig(personaUserId))
                .Returns(new PersonaConfig { PersonaUserId = personaUserId, DisplayName = "Test Persona", PreferredModelId = AiModels.AzureGpt4oThrivify, SystemPrompt = systemPrompt });

            var mockLogger = new Mock<ILogger<PersonaService>>();
            var mockNotificationService = new Mock<INotificationService>();

            // Mock dependencies needed by AI Model class constructors
            var mockConfiguration = new Mock<IConfiguration>();
            // Setup dummy config values to satisfy AI Model constructors during mock creation
            mockConfiguration.Setup(c => c["AzureOpenAI:Thrivify:ApiKey"]).Returns("dummy-key");
            mockConfiguration.Setup(c => c["AzureOpenAI:Thrivify:Endpoint"]).Returns("http://dummy.endpoint");
            mockConfiguration.Setup(c => c["AzureOpenAI:Ryan:ApiKey"]).Returns("dummy-key");
            mockConfiguration.Setup(c => c["AzureOpenAI:Ryan:Endpoint"]).Returns("http://dummy.endpoint");
            mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("dummy-key");
            mockConfiguration.Setup(c => c["Claude:ApiKey"]).Returns("dummy-claude-key");
            mockConfiguration.Setup(c => c["Gemini:ApiKey"]).Returns("dummy-key"); // Corrected key
            mockConfiguration.Setup(c => c["Vertex:Region"]).Returns("mock-region");
            mockConfiguration.Setup(c => c["Vertex:ServiceAccountJson"]).Returns(@"{
                ""type"": ""service_account"",
                ""project_id"": ""mock-project-id"",
                ""private_key_id"": ""mock-key-id"",
                ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMOCKKEY\n-----END PRIVATE KEY-----\n"",
                ""client_email"": ""mock@example.iam.gserviceaccount.com"",
                ""client_id"": ""123456789"",
                ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                ""token_uri"": ""https://oauth2.googleapis.com/token"",
                ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
                ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/mock@example.iam.gserviceaccount.com""
            }");
            // Add other necessary dummy config keys if more errors appear
            var mockAzureLogger = new Mock<ILogger<AzureAiProvider>>();
            var mockOpenAiLogger = new Mock<ILogger<OpenAiProvider>>();
            var mockClaudeLogger = new Mock<ILogger<ClaudeProvider>>();
            var mockGeminiLogger = new Mock<ILogger<GeminiProvider>>();
            var mockVertexLogger = new Mock<ILogger<VertexAiProvider>>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>(); // Keep this for other potential uses or remove if unused elsewhere

            // Setup a mock HttpClient to be returned by the factory if needed
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // No longer need the full mockServiceProvider setup for AI models if they take direct dependencies
            // var mockServiceProvider = new Mock<IServiceProvider>();
            // ... remove previous service provider setups ...

            // Mock AI Model Classes
            // For classes still using IServiceProvider:
            var mockServiceProviderForOthers = new Mock<IServiceProvider>();
            mockServiceProviderForOthers.Setup(sp => sp.GetService(typeof(IConfiguration))).Returns(mockConfiguration.Object);
            mockServiceProviderForOthers.Setup(sp => sp.GetService(typeof(ILogger<AzureAiProvider>))).Returns(mockAzureLogger.Object);
            mockServiceProviderForOthers.Setup(sp => sp.GetService(typeof(ILogger<OpenAiProvider>))).Returns(mockOpenAiLogger.Object); // Assuming OpenAiModels still uses SP
            mockServiceProviderForOthers.Setup(sp => sp.GetService(typeof(ILogger<ClaudeProvider>))).Returns(mockClaudeLogger.Object); // Assuming ClaudeModels still uses SP
            mockServiceProviderForOthers.Setup(sp => sp.GetService(typeof(ILogger<VertexAiProvider>))).Returns(mockVertexLogger.Object); // Assuming VertexAiModels still uses SP
            mockServiceProviderForOthers.Setup(sp => sp.GetService(typeof(IHttpClientFactory))).Returns(mockHttpClientFactory.Object);
            // Note: IHttpClientFactory setup might still be needed here if other models resolve it via SP

            var mockOpenAiModels = new Mock<OpenAiModels>(mockServiceProviderForOthers.Object); // Adjust if OpenAiModels constructor changes
            var mockAzureAiModels = new Mock<AzureAiModels>(mockServiceProviderForOthers.Object); // Adjust if AzureAiModels constructor changes
            var mockClaudeModels = new Mock<ClaudeModels>(mockServiceProviderForOthers.Object); // Adjust if ClaudeModels constructor changes
            // Instantiate GeminiModels mock with direct dependencies
            var mockGeminiModels = new Mock<GeminiModels>(mockConfiguration.Object, mockGeminiLogger.Object, mockHttpClientFactory.Object);
            var mockVertexAiModels = new Mock<VertexAiModels>(mockServiceProviderForOthers.Object); // Adjust if VertexAiModels constructor changes
// Use a simple function for the Azure model delegate instead of a mock
Func<string, List<ChatMessageDto>, Task<string?>> azureDelegate =
    (prompt, messages) => Task.FromResult<string?>(expectedResponse);

// Setup the AzureAiModels mock to return the simple delegate
mockAzureAiModels.Setup(m => m.Gpt4oThrivify)
                  .Returns(azureDelegate);

            var personaService = new PersonaService(
                mockDbContext.Object,
                mockPersonaConfigService.Object,
                mockLogger.Object,
                mockNotificationService.Object,
                mockOpenAiModels.Object,
                mockAzureAiModels.Object,
                mockClaudeModels.Object,
                mockGeminiModels.Object,
                mockVertexAiModels.Object
            );

            var triggeringMessageDto = new ChatMessageDto
            {
                Id = triggeringMessageEntity.Id,
                Text = triggeringMessageEntity.Text,
                Date = triggeringMessageEntity.Date,
                UserId = triggeringMessageEntity.UserId,
                UserFirstName = regularUser.FirstName,
                UserLastName = regularUser.LastName,
                ChatroomId = triggeringMessageEntity.ChatroomId,
                Role = MessageRole.User
            };

            // Act
            await personaService.GenerateResponseAsync(chatroomId, triggeringMessageDto);

            // Assert
            // 1. Verify the Azure models getter was accessed (we can't verify the delegate call directly anymore)
            mockAzureAiModels.VerifyGet(m => m.Gpt4oThrivify, Times.Once());

            // 2. Verify other AI models were NOT called (optional but good practice)
            mockOpenAiModels.Verify(m => m.Gpt4oLatest(It.IsAny<string>(), It.IsAny<List<ChatMessageDto>>()), Times.Never());
            mockClaudeModels.Verify(m => m.Claude37Sonnet(It.IsAny<string>(), It.IsAny<List<ChatMessageDto>>()), Times.Never());
            // ... verify other models if necessary

            // 3. Verify SaveChangesAsync was called
            mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());

            // 4. Verify the message was added to the context (check callback variable)
            addedMessage.Should().NotBeNull();
            if (addedMessage != null)
            {
                addedMessage.Text.Should().Be(expectedResponse);
                addedMessage.UserId.Should().Be(personaUserId);
                addedMessage.ChatroomId.Should().Be(chatroomId);
            }

            // 5. Verify notification was sent
            mockNotificationService.Verify(n => n.SendMessageToGroupAsync(
                chatroomId.ToString(),
                chatroomId,
                It.Is<ChatMessageDto>(dto =>
                    dto.Text == expectedResponse &&
                    dto.UserId == personaUserId &&
                    dto.Role == MessageRole.Assistant &&
                    dto.UserFirstName == aiUser.FirstName // Ensure correct user details
                )), Times.Once());
        }
    
        [Fact]
        public async Task GenerateResponseAsync_WhenAzureGpt45IsPreferred_CallsAzureGpt45Ryan()
        {
            // Arrange
            const int chatroomId = 1;
            const int personaUserId = 999;
            const string expectedResponse = "Mocked Azure GPT-4.5 Preview Response";
            const string systemPrompt = "You are a helpful assistant.";
    
            var regularUser = new User { Id = 1, FirstName = "Regular", LastName = "User", Email = "user@example.com", PasswordHash = "hash" };
            var aiUser = new User { Id = personaUserId, FirstName = "AI", LastName = "Assistant", Email = "ai@example.com", PasswordHash = "aihash", IsPersona = true };
            var users = new List<User> { regularUser, aiUser };
    
            var chatroom = new Chatroom { Id = chatroomId, Title = "Azure Test", PersonaUserId = personaUserId, PersonaUser = aiUser };
            chatroom.Users.Add(regularUser);
            chatroom.Users.Add(aiUser);
            var chatrooms = new List<Chatroom> { chatroom };
    
            var triggeringMessageEntity = new ChatMessage
            {
                Id = 1, ChatroomId = chatroomId, UserId = 1, Text = "Hello Azure!", Date = DateTime.UtcNow.AddMinutes(-1), User = regularUser, Chatroom = chatroom
            };
            var chatMessages = new List<ChatMessage> { triggeringMessageEntity };
            chatroom.Chats.Add(triggeringMessageEntity);
            regularUser.Messages.Add(triggeringMessageEntity);
    
            var usersMockDbSet = users.AsQueryable().BuildMockDbSet();
            var chatroomsMockDbSet = chatrooms.AsQueryable().BuildMockDbSet();
            var chatMessagesMockDbSet = chatMessages.AsQueryable().BuildMockDbSet();
    
            var mockDbContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            mockDbContext.Setup(c => c.Users).Returns(usersMockDbSet.Object);
            mockDbContext.Setup(c => c.Chatrooms).Returns(chatroomsMockDbSet.Object);
            mockDbContext.Setup(c => c.ChatMessages).Returns(chatMessagesMockDbSet.Object);
            ChatMessage? addedMessage = null;
            chatMessagesMockDbSet.Setup(d => d.AddAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ChatMessage, CancellationToken>((msg, _) => {
                    msg.Id = chatMessages.Count + 1; // Simulate ID generation
                    msg.User = users.First(u => u.Id == msg.UserId);
                    msg.Chatroom = chatrooms.First(c => c.Id == msg.ChatroomId);
                    chatMessages.Add(msg);
                    addedMessage = msg;
                })
                // Return default EntityEntry as it's not used and mocking is problematic
                .Returns((ChatMessage entity, CancellationToken _) => ValueTask.FromResult<EntityEntry<ChatMessage>>(default!));
    
            mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    
            var mockPersonaConfigService = new Mock<IPersonaConfigService>();
            mockPersonaConfigService.Setup(s => s.GetConfig(personaUserId))
                .Returns(new PersonaConfig { PersonaUserId = personaUserId, DisplayName = "Test Persona", PreferredModelId = AiModels.AzureGpt45PreviewRyan, SystemPrompt = systemPrompt });
    
            var mockLogger = new Mock<ILogger<PersonaService>>();
            var mockNotificationService = new Mock<INotificationService>();
    
            // Mock dependencies needed by AI Model class constructors
            var mockConfiguration = new Mock<IConfiguration>();
            // Setup dummy config values to satisfy AI Model constructors during mock creation
            mockConfiguration.Setup(c => c["AzureOpenAI:Thrivify:ApiKey"]).Returns("dummy-key");
            mockConfiguration.Setup(c => c["AzureOpenAI:Thrivify:Endpoint"]).Returns("http://dummy.endpoint");
            mockConfiguration.Setup(c => c["AzureOpenAI:Ryan:ApiKey"]).Returns("dummy-key");
            mockConfiguration.Setup(c => c["AzureOpenAI:Ryan:Endpoint"]).Returns("http://dummy.endpoint");
            mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("dummy-key");
            mockConfiguration.Setup(c => c["Claude:ApiKey"]).Returns("dummy-claude-key");
            mockConfiguration.Setup(c => c["Gemini:ApiKey"]).Returns("dummy-key");
            mockConfiguration.Setup(c => c["Vertex:Region"]).Returns("mock-region");
            mockConfiguration.Setup(c => c["Vertex:ServiceAccountJson"]).Returns(@"{
                ""type"": ""service_account"",
                ""project_id"": ""mock-project-id"",
                ""private_key_id"": ""mock-key-id"",
                ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMOCKKEY\n-----END PRIVATE KEY-----\n"",
                ""client_email"": ""mock@example.iam.gserviceaccount.com"",
                ""client_id"": ""123456789"",
                ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                ""token_uri"": ""https://oauth2.googleapis.com/token"",
                ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
                ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/mock@example.iam.gserviceaccount.com""
            }");
            
            var mockAzureLogger = new Mock<ILogger<AzureAiProvider>>();
            var mockOpenAiLogger = new Mock<ILogger<OpenAiProvider>>();
            var mockClaudeLogger = new Mock<ILogger<ClaudeProvider>>();
            var mockGeminiLogger = new Mock<ILogger<GeminiProvider>>();
            var mockVertexLogger = new Mock<ILogger<VertexAiProvider>>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
    
            // Setup a mock HttpClient to be returned by the factory
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
    
            // Mock service provider for models that still use it
            var mockServiceProviderForOthers = new Mock<IServiceProvider>();
            mockServiceProviderForOthers.Setup(sp => sp.GetService(typeof(IConfiguration))).Returns(mockConfiguration.Object);
            mockServiceProviderForOthers.Setup(sp => sp.GetService(typeof(ILogger<AzureAiProvider>))).Returns(mockAzureLogger.Object);
            mockServiceProviderForOthers.Setup(sp => sp.GetService(typeof(ILogger<OpenAiProvider>))).Returns(mockOpenAiLogger.Object);
            mockServiceProviderForOthers.Setup(sp => sp.GetService(typeof(ILogger<ClaudeProvider>))).Returns(mockClaudeLogger.Object);
            mockServiceProviderForOthers.Setup(sp => sp.GetService(typeof(ILogger<VertexAiProvider>))).Returns(mockVertexLogger.Object);
            mockServiceProviderForOthers.Setup(sp => sp.GetService(typeof(IHttpClientFactory))).Returns(mockHttpClientFactory.Object);
    
            var mockOpenAiModels = new Mock<OpenAiModels>(mockServiceProviderForOthers.Object);
            var mockAzureAiModels = new Mock<AzureAiModels>(mockServiceProviderForOthers.Object);
            var mockClaudeModels = new Mock<ClaudeModels>(mockServiceProviderForOthers.Object);
            var mockGeminiModels = new Mock<GeminiModels>(mockConfiguration.Object, mockGeminiLogger.Object, mockHttpClientFactory.Object);
            var mockVertexAiModels = new Mock<VertexAiModels>(mockServiceProviderForOthers.Object);
    
            // Setup the delegate for Gpt45PreviewRyan
            Func<string, List<ChatMessageDto>, Task<string?>> azureDelegate =
                (prompt, messages) => Task.FromResult<string?>(expectedResponse);
    
            // Setup the AzureAiModels mock to return the delegate for Gpt45PreviewRyan
            mockAzureAiModels.Setup(m => m.Gpt45PreviewRyan)
                           .Returns(azureDelegate);
    
            var personaService = new PersonaService(
                mockDbContext.Object,
                mockPersonaConfigService.Object,
                mockLogger.Object,
                mockNotificationService.Object,
                mockOpenAiModels.Object,
                mockAzureAiModels.Object,
                mockClaudeModels.Object,
                mockGeminiModels.Object,
                mockVertexAiModels.Object
            );
    
            var triggeringMessageDto = new ChatMessageDto
            {
                Id = triggeringMessageEntity.Id,
                Text = triggeringMessageEntity.Text,
                Date = triggeringMessageEntity.Date,
                UserId = triggeringMessageEntity.UserId,
                UserFirstName = regularUser.FirstName,
                UserLastName = regularUser.LastName,
                ChatroomId = triggeringMessageEntity.ChatroomId,
                Role = MessageRole.User
            };
    
            // Act
            await personaService.GenerateResponseAsync(chatroomId, triggeringMessageDto);
    
            // Assert
            // 1. Verify the Azure models getter was accessed
            mockAzureAiModels.VerifyGet(m => m.Gpt45PreviewRyan, Times.Once());
    
            // 2. Verify other AI models were NOT called
            mockOpenAiModels.Verify(m => m.Gpt4oLatest(It.IsAny<string>(), It.IsAny<List<ChatMessageDto>>()), Times.Never());
            mockAzureAiModels.VerifyGet(m => m.Gpt4oThrivify, Times.Never());
            mockClaudeModels.Verify(m => m.Claude37Sonnet(It.IsAny<string>(), It.IsAny<List<ChatMessageDto>>()), Times.Never());
    
            // 3. Verify SaveChangesAsync was called
            mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    
            // 4. Verify the message was added to the context
            addedMessage.Should().NotBeNull();
            if (addedMessage != null)
            {
                addedMessage.Text.Should().Be(expectedResponse);
                addedMessage.UserId.Should().Be(personaUserId);
                addedMessage.ChatroomId.Should().Be(chatroomId);
            }
    
            // 5. Verify notification was sent
            mockNotificationService.Verify(n => n.SendMessageToGroupAsync(
                chatroomId.ToString(),
                chatroomId,
                It.Is<ChatMessageDto>(dto =>
                    dto.Text == expectedResponse &&
                    dto.UserId == personaUserId &&
                    dto.Role == MessageRole.Assistant &&
                    dto.UserFirstName == aiUser.FirstName
                )), Times.Once());
        }
    }
}