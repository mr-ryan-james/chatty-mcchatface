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
    public class VertexAiProviderIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;
        private readonly IConfiguration _config;
        private readonly ILogger<VertexAiProvider> _logger;

        public VertexAiProviderIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _config = _fixture.Configuration;
            _logger = _fixture.Services.GetRequiredService<ILogger<VertexAiProvider>>();
        }

        [Fact(Skip = "Requires secrets configuration and live API calls")]
        public async Task GetCompletionAsync_WithValidInput_ReturnsResponse()
        {
            // Arrange
            var projectId = _config["Vertex:ProjectId"];
            var region = _config["Vertex:Region"];
            var serviceAccountJson = _config["Vertex:ServiceAccountJson"];
            
            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(region) || string.IsNullOrEmpty(serviceAccountJson))
            {
                Assert.Fail("Vertex AI configuration not found in User Secrets.");
                return;
            }

            var provider = new VertexAiProvider(_config, _logger);
            var modelId = AiModels.Claude37SonnetVertex;
            var prompt = "Say hello.";
            var history = new List<ChatMessageDto>();

            // Act
            var result = await provider.GetCompletionAsync(prompt, history, modelId);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
        }

        [Fact(Skip = "Requires secrets configuration and live API calls")]
        public async Task GetCompletionAsync_WithHistory_ProvidesContinuity()
        {
            // Arrange
            var projectId = _config["Vertex:ProjectId"];
            var region = _config["Vertex:Region"];
            var serviceAccountJson = _config["Vertex:ServiceAccountJson"];
            
            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(region) || string.IsNullOrEmpty(serviceAccountJson))
            {
                Assert.Fail("Vertex AI configuration not found in User Secrets.");
                return;
            }

            var provider = new VertexAiProvider(_config, _logger);
            var modelId = AiModels.Claude37SonnetVertex;
            var prompt = "Can you remember what I just asked?";
            var history = new List<ChatMessageDto>
            {
                new ChatMessageDto
                {
                    Text = "Who discovered penicillin?",
                    UserFirstName = "Test",
                    UserLastName = "User",
                    Role = MessageRole.User
                },
                new ChatMessageDto
                {
                    Text = "Alexander Fleming discovered penicillin in 1928.",
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