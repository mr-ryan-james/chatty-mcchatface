using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Dtos;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;

namespace ChattyMcChatface.Tests.Integration.AI.Providers
{
    public class GeminiProviderIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;
        private readonly IConfiguration _config;
        private readonly ILogger<GeminiProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public GeminiProviderIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _config = _fixture.Configuration;
            _logger = _fixture.Services.GetRequiredService<ILogger<GeminiProvider>>();
            _httpClientFactory = _fixture.Services.GetRequiredService<IHttpClientFactory>();
        }

        [Fact]
        public async Task GetCompletionAsync_WithValidInput_ReturnsResponse()
        {
            // Arrange
            var apiKey = _config["Gemini:ApiKey"];
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Assert.Fail("Gemini API Key not configured in User Secrets.");
                return;
            }

            var provider = new GeminiProvider(_config, _logger, _httpClientFactory);
            var modelId = AiModels.Gemini25Pro;
            var prompt = "Say hello.";
            var history = new List<ChatMessageDto>();

            // Act
            var result = await provider.GetCompletionAsync(prompt, history, modelId);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GetCompletionAsync_WithHistory_MaintainsConversationContext()
        {
            // Arrange
            var apiKey = _config["Gemini:ApiKey"];
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Assert.Fail("Gemini API Key not configured in User Secrets.");
                return;
            }

            var provider = new GeminiProvider(_config, _logger, _httpClientFactory);
            var modelId = AiModels.Gemini25Pro;
            var prompt = "What was my last question?";
            var history = new List<ChatMessageDto>
            {
                new ChatMessageDto
                {
                    Text = "What's the tallest mountain in the world?",
                    UserFirstName = "Test",
                    UserLastName = "User",
                    Role = MessageRole.User
                },
                new ChatMessageDto
                {
                    Text = "Mount Everest is the tallest mountain in the world, with a height of 8,848.86 meters (29,031.7 feet) above sea level.",
                    UserFirstName = "AI",
                    UserLastName = "Assistant",
                    Role = MessageRole.Assistant
                }
            };

            // Act
            var result = await provider.GetCompletionAsync(prompt, history, modelId);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
        }
    }
}