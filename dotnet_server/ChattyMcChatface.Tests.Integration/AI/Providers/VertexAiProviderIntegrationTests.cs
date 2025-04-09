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

        [Fact]
        public async Task GetCompletionAsync_SimpleTest_ReturnsExpectedResponse()
        {
            // This test runs a simple check of the VertexAiProvider implementation
            try
            {
                // Arrange
                var region = _config["Vertex:Region"];
                var serviceAccountJson = _config["Vertex:ServiceAccountJson"];
                
                if (string.IsNullOrEmpty(region) || string.IsNullOrEmpty(serviceAccountJson))
                {
                    Assert.Fail("Vertex AI configuration is missing from user secrets. Please configure the credentials to run this test.");
                    return;
                }

                // Create provider
                var provider = new VertexAiProvider(_config, _logger);
                
                // Use a carefully constructed prompt that should have a deterministic answer
                var systemPrompt = "You are a helpful assistant that answers questions briefly and accurately.";
                var history = new List<ChatMessageDto>
                {
                    new ChatMessageDto
                    {
                        Text = "What is 2+2?",
                        UserFirstName = "Test",
                        UserLastName = "User",
                        Role = MessageRole.User
                    }
                };
                
                // Act
                var result = await provider.GetCompletionAsync(systemPrompt, history, AiModels.Claude37SonnetVertex);
                
                // Assert
                result.Should().NotBeNullOrWhiteSpace("because the API should return a response for a simple question");
                
                // Check the response contains "4" - this should be reliable for the simple math question
                result.Should().Contain("4", "because the answer to 2+2 should include the number 4");
                
                // Test with more complex interaction
                history.Add(new ChatMessageDto
                {
                    Text = result,
                    UserFirstName = "AI",
                    UserLastName = "Assistant",
                    Role = MessageRole.Assistant
                });
                
                history.Add(new ChatMessageDto
                {
                    Text = "Is that correct?",
                    UserFirstName = "Test",
                    UserLastName = "User",
                    Role = MessageRole.User
                });
                
                // Act - second call to check conversation continuity works
                var followupResult = await provider.GetCompletionAsync(systemPrompt, history, AiModels.Claude37SonnetVertex);
                
                // Assert
                followupResult.Should().NotBeNullOrWhiteSpace("because the API should maintain conversation continuity");
                followupResult.Should().ContainAny(new[] { "yes", "Yes", "correct", "Correct", "right", "Right" }, 
                    "because the model should confirm that 2+2=4 is correct");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Test error: {ex.GetType().Name}: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                throw; // Re-throw for xUnit
            }
        }

        [Fact]
        public async Task GetCompletionAsync_WithContentionDetection_ReturnsValidResponse()
        {
            // Arrange
            var region = _config["Vertex:Region"];
            var serviceAccountJson = _config["Vertex:ServiceAccountJson"];
            
            if (string.IsNullOrEmpty(region) || string.IsNullOrEmpty(serviceAccountJson))
            {
                Assert.Fail("Vertex AI configuration is missing from user secrets. Please configure the credentials to run this test.");
                return;
            }

            var provider = new VertexAiProvider(_config, _logger);
            var modelId = AiModels.Claude37SonnetVertex;
            
            // In this test, we'll evaluate the model's ability to detect contradictions or problems
            var systemPrompt = "You are a helpful assistant that points out contradictions when they exist.";
            var history = new List<ChatMessageDto>
            {
                new ChatMessageDto
                {
                    Text = "The sky is red. The sky is blue.",
                    UserFirstName = "Test",
                    UserLastName = "User",
                    Role = MessageRole.User
                }
            };

            // Act
            var result = await provider.GetCompletionAsync(systemPrompt, history, modelId);
            
            // Assert
            result.Should().NotBeNullOrWhiteSpace("because the API should return a response for a simple contradiction");
            
            // The response should mention contradiction or conflicting statements
            result.Should().ContainAny(
                new[] { "contradiction", "contradictory", "conflict", "inconsistent", "both red and blue", "cannot be both" },
                "because the model should identify the contradiction about the sky's color"
            );
        }
    }
}