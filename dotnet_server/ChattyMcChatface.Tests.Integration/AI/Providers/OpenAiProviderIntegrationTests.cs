using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Dtos;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ChattyMcChatface.Tests.Integration.AI.Providers
{
    public class OpenAiProviderIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;
        private readonly IConfiguration _config;
        private readonly ILogger<OpenAiProvider> _logger;

        public OpenAiProviderIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _config = _fixture.Services.GetRequiredService<IConfiguration>();
            _logger = _fixture.Services.GetRequiredService<ILogger<OpenAiProvider>>();
        }

        [Fact]
        public async Task GetCompletionAsync_WithValidInput_ReturnsResponse()
        {
            // Arrange
            var apiKey = _config["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                Assert.Fail("OpenAI API Key not configured in User Secrets.");
                return;
            }

            var provider = new OpenAiProvider(_config, _logger);
            var modelId = AiModels.OpenAiGpt4oLatest;
            var prompt = "Say hello.";
            var history = new List<ChatMessageDto>();

            // Act
            var result = await provider.GetCompletionAsync(prompt, history, modelId);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GetCompletionAsync_WithHistory_ReturnsContextualResponse()
        {
            // Arrange
            var apiKey = _config["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                Assert.Fail("OpenAI API Key not configured in User Secrets.");
                return;
            }

            var provider = new OpenAiProvider(_config, _logger);
            var modelId = AiModels.OpenAiGpt4oLatest;
            var prompt = "What did I just ask you?";
            var history = new List<ChatMessageDto>
            {
                new ChatMessageDto
                {
                    Text = "What's the capital of France?",
                    UserFirstName = "Test",
                    UserLastName = "User",
                    Role = MessageRole.User
                },
                new ChatMessageDto
                {
                    Text = "The capital of France is Paris.",
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