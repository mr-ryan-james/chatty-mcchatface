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
    public class AzureAiProviderIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;
        private readonly IConfiguration _config;
        private readonly ILogger<AzureAiProvider> _logger;

        public AzureAiProviderIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _config = _fixture.Configuration;
            _logger = _fixture.Services.GetRequiredService<ILogger<AzureAiProvider>>();
        }

        [Fact(Skip = "Requires secrets configuration and live API calls")]
        public async Task GetCompletionAsync_WithValidInput_ReturnsResponse()
        {
            // Arrange
            var apiKey = _config["Azure:ApiKey"];
            var endpoint = _config["Azure:Endpoint"];
            
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint))
            {
                Assert.Fail("Azure AI configuration not found in User Secrets.");
                return;
            }

            var provider = new AzureAiProvider(_config, _logger);
            var modelId = AiModels.AzureGpt4oThrivify;
            var prompt = "Say hello.";
            var history = new List<ChatMessageDto>();

            // Act
            var result = await provider.GetCompletionAsync(prompt, history, modelId);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
        }

        [Fact(Skip = "Requires secrets configuration and live API calls")]
        public async Task GetCompletionAsync_WithHistory_MaintainsContext()
        {
            // Arrange
            var apiKey = _config["Azure:ApiKey"];
            var endpoint = _config["Azure:Endpoint"];
            
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint))
            {
                Assert.Fail("Azure AI configuration not found in User Secrets.");
                return;
            }

            var provider = new AzureAiProvider(_config, _logger);
            var modelId = AiModels.AzureGpt4oThrivify;
            var prompt = "What was the question I just asked?";
            var history = new List<ChatMessageDto>
            {
                new ChatMessageDto
                {
                    Text = "What's the capital of Italy?",
                    UserFirstName = "Test",
                    UserLastName = "User",
                    Role = MessageRole.User
                },
                new ChatMessageDto
                {
                    Text = "The capital of Italy is Rome.",
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