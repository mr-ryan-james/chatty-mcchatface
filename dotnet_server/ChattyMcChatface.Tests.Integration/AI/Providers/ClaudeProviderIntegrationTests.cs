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
    public class ClaudeProviderIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;
        private readonly IConfiguration _config;
        private readonly ILogger<ClaudeProvider> _logger;

        public ClaudeProviderIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _config = _fixture.Configuration;
            _logger = _fixture.Services.GetRequiredService<ILogger<ClaudeProvider>>();
        }

        [Fact]
        public async Task GetCompletionAsync_WithValidInput_ReturnsResponse()
        {
            // Arrange
            var apiKey = _config["Claude:ApiKey"];
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Assert.Fail("Claude API Key not configured in User Secrets.");
                return;
            }

            var provider = new ClaudeProvider(_config, _logger);
            var modelId = AiModels.Claude37Sonnet;
            var prompt = "Say hello.";
            var history = new List<ChatMessageDto>();

            // Act
            var result = await provider.GetCompletionAsync(prompt, history, modelId);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task GetCompletionAsync_WithHistory_ProcessesConversationContext()
        {
            // Arrange
            var apiKey = _config["Claude:ApiKey"];
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Assert.Fail("Claude API Key not configured in User Secrets.");
                return;
            }

            var provider = new ClaudeProvider(_config, _logger);
            var modelId = AiModels.Claude37Sonnet;
            var prompt = "What was my previous question?";
            var history = new List<ChatMessageDto>
            {
                new ChatMessageDto
                {
                    Text = "Who wrote 'Pride and Prejudice'?",
                    UserFirstName = "Test",
                    UserLastName = "User",
                    Role = MessageRole.User
                },
                new ChatMessageDto
                {
                    Text = "Jane Austen wrote 'Pride and Prejudice'.",
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