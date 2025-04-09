using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Services.AI.Azure;
using ChattyMcChatface.Core.Dtos;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ChattyMcChatface.Tests.Integration.AI.Providers
{
    public class AzureAiProviderIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;
        private readonly IConfiguration _config;
        private readonly IServiceProvider _serviceProvider;
        private readonly Func<string, List<ChatMessageDto>, Task<string?>> _thrivifyProvider;
        private readonly Func<string, List<ChatMessageDto>, Task<string?>> _ryanProvider;

        public AzureAiProviderIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _config = _fixture.Configuration;
            _serviceProvider = _fixture.Services;

            // Validate configuration exists
            ValidateAzureConfiguration();

            // Initialize AzureAiModels to get provider delegates
            var azureModels = new AzureAiModels(_serviceProvider);
            _thrivifyProvider = azureModels.Gpt4oThrivify;
            _ryanProvider = azureModels.Gpt45PreviewRyan;
        }

        private void ValidateAzureConfiguration()
        {
            // Check Thrivify configuration
            var thrivifyApiKey = _config["AzureOpenAI:Thrivify:ApiKey"];
            var thrivifyEndpoint = _config["AzureOpenAI:Thrivify:Endpoint"];
            
            // Check Ryan configuration
            var ryanApiKey = _config["AzureOpenAI:Ryan:ApiKey"];
            var ryanEndpoint = _config["AzureOpenAI:Ryan:Endpoint"];
            
            if (string.IsNullOrEmpty(thrivifyApiKey) || string.IsNullOrEmpty(thrivifyEndpoint))
            {
                Assert.Fail("Azure AI Thrivify configuration not found in User Secrets.");
                return;
            }
            
            if (string.IsNullOrEmpty(ryanApiKey) || string.IsNullOrEmpty(ryanEndpoint))
            {
                Assert.Fail("Azure AI Ryan configuration not found in User Secrets.");
                return;
            }
        }

        [Fact(Skip = "Requires secrets configuration and live API calls")]
        public async Task GetCompletionAsync_WithThrivifyProvider_ReturnsResponse()
        {
            // Arrange
            var prompt = "Say hello.";
            var history = new List<ChatMessageDto>();

            // Act
            var result = await _thrivifyProvider(prompt, history);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
        }

        [Fact(Skip = "Requires secrets configuration and live API calls")]
        public async Task GetCompletionAsync_WithRyanProvider_ReturnsResponse()
        {
            // Arrange
            var prompt = "Say hello.";
            var history = new List<ChatMessageDto>();

            // Act
            var result = await _ryanProvider(prompt, history);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
        }

        [Fact(Skip = "Requires secrets configuration and live API calls")]
        public async Task GetCompletionAsync_WithHistory_MaintainsContext()
        {
            // Arrange
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
            var result = await _thrivifyProvider(prompt, history);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
        }
    }
}