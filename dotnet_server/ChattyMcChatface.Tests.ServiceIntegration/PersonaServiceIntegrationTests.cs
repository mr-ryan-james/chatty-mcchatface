using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Services;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Data;
using ChattyMcChatface.Data.Entities;
using ChattyMcChatface.Core.Services.AI;
using System.Text.Json;

namespace ChattyMcChatface.Tests.ServiceIntegration
{
    public class PersonaServiceIntegrationTests
    {
        // Mocks and dependencies
        private readonly Mock<ILogger<PersonaService>> _mockLogger;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<OpenAiProvider> _mockOpenAiProvider;
        private readonly Mock<GeminiProvider> _mockGeminiProvider;
        private readonly Mock<ClaudeProvider> _mockClaudeProvider;
        private readonly Mock<VertexAiProvider> _mockVertexAiProvider;
        private readonly IConfiguration _configuration; // Use real configuration
        private readonly Mock<ILogger<AzureAiProvider>> _mockAzureLogger;
        private readonly Mock<AzureAiProvider> _mockAzureAiProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        // Use real DbContext for in-memory testing
        private readonly AppDbContext _dbContext;

        private readonly PersonaService _service;

        public PersonaServiceIntegrationTests()
        {
            // Build configuration that includes user secrets
            _configuration = new ConfigurationBuilder()
                .AddUserSecrets<PersonaServiceIntegrationTests>()
                .Build();

            // Create a real HttpClientFactory
            var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
            _httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            // Setup in-memory database options for the real context
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new AppDbContext(options);

            _mockLogger = new Mock<ILogger<PersonaService>>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockAzureLogger = new Mock<ILogger<AzureAiProvider>>();

            // Providers with strict behavior, using real configuration and real IHttpClientFactory where needed
            _mockOpenAiProvider = new Mock<OpenAiProvider>(MockBehavior.Strict, _configuration, Mock.Of<ILogger<OpenAiProvider>>());
            _mockGeminiProvider = new Mock<GeminiProvider>(MockBehavior.Strict, _configuration, Mock.Of<ILogger<GeminiProvider>>(), _httpClientFactory);
            _mockClaudeProvider = new Mock<ClaudeProvider>(MockBehavior.Strict, _configuration, Mock.Of<ILogger<ClaudeProvider>>());
            _mockVertexAiProvider = new Mock<VertexAiProvider>(MockBehavior.Strict, _configuration, Mock.Of<ILogger<VertexAiProvider>>(), _httpClientFactory);

            // Azure provider mock needs real config values for constructor even if mocked later
            var azureApiKey = _configuration["AzureOpenAI:Test:ApiKey"] ?? "dummy_key";
            var azureEndpoint = _configuration["AzureOpenAI:Test:Endpoint"] ?? "http://dummy.endpoint";
            _mockAzureAiProvider = new Mock<AzureAiProvider>(MockBehavior.Strict, azureApiKey, azureEndpoint, _mockAzureLogger.Object);

            // Default provider responses
            _mockGeminiProvider.Setup(p => p.GetCompletionAsync(It.IsAny<string>(), It.IsAny<List<ChatMessageDto>>(), It.IsAny<string>()))
                .ReturnsAsync("Gemini response");
            _mockOpenAiProvider.Setup(p => p.GetCompletionAsync(It.IsAny<string>(), It.IsAny<List<ChatMessageDto>>(), It.IsAny<string>()))
                .ReturnsAsync("OpenAI response");
            _mockClaudeProvider.Setup(p => p.GetCompletionAsync(It.IsAny<string>(), It.IsAny<List<ChatMessageDto>>(), It.IsAny<string>()))
                .ReturnsAsync("Claude response");
            _mockVertexAiProvider.Setup(p => p.GetCompletionAsync(It.IsAny<string>(), It.IsAny<List<ChatMessageDto>>(), It.IsAny<string>()))
                .ReturnsAsync("Vertex response");

            // Instantiate PersonaService with mocks
            _service = new PersonaService(
                _dbContext,
                _mockLogger.Object,
                _mockNotificationService.Object,
                _mockOpenAiProvider.Object,
                _configuration,
                _mockAzureLogger.Object,
                _mockClaudeProvider.Object,
                _mockGeminiProvider.Object,
                _mockVertexAiProvider.Object
            );
        }

        [Fact]
        public async Task GenerateResponseAsync_WithValidJsonResponse_SavesOriginalJson()
        {
            // Arrange
            var personaUser = new User
            {
                Id = 1001,
                FirstName = "Persona",
                LastName = "Bot",
                Email = $"val_persona_{Guid.NewGuid()}@example.com",
                PasswordHash = "DUMMY_HASH",
                IsPersona = true,
                SystemPrompt = "You are a helpful AI.",
                PreferredModelId = "gemini-2.5-pro-preview-03-25"
            };
            var testUser = new User
            {
                Id = 1,
                FirstName = "Test",
                LastName = "User",
                Email = $"val_test_{Guid.NewGuid()}@example.com",
                PasswordHash = "DUMMY_HASH",
                IsPersona = false
            };
            var chatroom = new Chatroom
            {
                Id = 1,
                Title = "Test Chatroom",
                PersonaUserId = personaUser.Id,
                Users = new List<User> { testUser, personaUser }
            };
            _dbContext.Users.AddRange(testUser, personaUser);
            _dbContext.Chatrooms.Add(chatroom);
            _dbContext.SaveChanges();

            var validJson = """{"ai_persona":"Test Persona","response_type":"Test Type","message":"Valid test message."}""";
            _mockGeminiProvider.Setup(p => p.GetCompletionAsync(It.IsAny<string>(), It.IsAny<List<ChatMessageDto>>(), It.IsAny<string>()))
                .ReturnsAsync(validJson);

            var userMessageDto = new ChatMessageDto
            {
                UserId = testUser.Id,
                ChatroomId = chatroom.Id,
                Text = "Test message",
                Role = MessageRole.User,
                UserFirstName = testUser.FirstName,
                UserLastName = testUser.LastName
            };

            // Act
            await _service.GenerateResponseAsync(chatroom.Id, userMessageDto);

            // Assert
            var personaMessage = _dbContext.ChatMessages
                .Where(m => m.ChatroomId == chatroom.Id && m.UserId == personaUser.Id)
                .OrderByDescending(m => m.Date)
                .FirstOrDefault();

            personaMessage.Should().NotBeNull();
            personaMessage!.Text.Should().Be(validJson);
        }

        // --- JSON Validation Helper and Tests ---

        // Modified RunValidationTest to use _usersData and _chatMessagesData
        private async Task RunValidationTest(string modelIdToMock, string? mockResponse, string? expectedJson)
        {
            // Arrange
            var chatroomId = 1;
            var testUserId = 10;
            var personaUserId = 100; // Example ID for the persona

            // Ensure User objects have necessary properties set
            var testUser = new User { Id = testUserId, FirstName = "Test", LastName = "User", Email = "test@example.com", PasswordHash = "hashed_password" };
            var personaUser = new User { Id = personaUserId, FirstName = "Persona", LastName = "User", Email = "persona@example.com", PasswordHash = "persona_hash", IsPersona = true, PreferredModelId = modelIdToMock, SystemPrompt = "Prompt" };
            // Seed directly into context
            await _dbContext.Users.AddRangeAsync(testUser, personaUser);
            // Seed Chatroom if needed by PersonaService logic
            var chatroom = new Chatroom { Id = chatroomId, PersonaUserId = personaUserId };
            await _dbContext.Chatrooms.AddAsync(chatroom);
            await _dbContext.SaveChangesAsync();

            // Ensure ChatMessageDto has necessary properties set
            var userMessageDto = new ChatMessageDto { UserId = testUserId, Text = "User message", Date = DateTime.UtcNow, UserFirstName="Test", UserLastName="User", ChatroomId=chatroomId, Id=999, Role=MessageRole.User };

            // Setup the specific provider mock
            IAiProvider mockProvider = modelIdToMock switch
            {
                "gemini-2.5-pro-preview-03-25" => _mockGeminiProvider.Object,
                // Add cases for other providers used in tests
                _ => throw new ArgumentException("Invalid model ID for mocking")
            };
            // Ensure the setup matches the parameters used in PersonaService
            _mockGeminiProvider.Setup(p => p.GetCompletionAsync(It.IsAny<string>(), It.IsAny<List<ChatMessageDto>>(), It.IsAny<string>()))
                        .ReturnsAsync(mockResponse);

            // Act
            await _service.GenerateResponseAsync(chatroomId, userMessageDto);

            // Assert
            // Verify using the in-memory list _chatMessagesData
            var savedMessages = await _dbContext.ChatMessages.Where(m => m.ChatroomId == chatroomId).ToListAsync();
            savedMessages.Should().HaveCount(1); // Expecting only the persona's response
            var personaMessage = savedMessages.First();
            personaMessage.UserId.Should().Be(personaUserId);
            personaMessage.Should().NotBeNull(); // Add this assertion
            personaMessage!.Text.Should().Be(expectedJson); // Use null-forgiving operator (!)

            // Verify notification was sent (assuming _mockNotificationService is set up)
            _mockNotificationService.Verify(n => n.SendMessageToGroupAsync(
                chatroomId.ToString(),
                chatroomId,
                It.Is<ChatMessageDto>(dto => dto.Text == expectedJson)),
                Times.Once);
        }

        [Fact]
        public async Task GenerateResponseAsync_WithInvalidJsonResponse_DoesNotSaveOriginalJson()
        {
            string invalidJson = "{invalid json}";
            string expectedFallbackJson = JsonSerializer.Serialize(new AiStructuredResponseDto { AiPersona = "Persona", ResponseType = "Error", Message = "[AI response could not be parsed or was empty]" });
            await RunValidationTest(AiModels.Gemini25Pro, invalidJson, expectedFallbackJson);
        }

        [Fact]
        public async Task GenerateResponseAsync_WithValidJsonResponse_SavesOriginalJson_Validation()
        {
            string validJson = """{"ai_persona":"Test Persona","response_type":"Test Type","message":"Valid test message."}""";
            await RunValidationTest(AiModels.Gemini25Pro, validJson, validJson);
        }
    }
}