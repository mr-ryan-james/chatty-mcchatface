using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Services.AI.Vertex;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChattyMcChatface.Tests.Unit.AI.Factories
{
    public class VertexAiProviderFactoryTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<VertexAiProvider>> _mockLogger;
        private readonly string _testModelId = AiModels.Claude37SonnetVertex;
        private readonly string _testSystemPrompt = "Test system prompt";
        private readonly List<ChatMessageDto> _testHistory = new List<ChatMessageDto>();

        public VertexAiProviderFactoryTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<VertexAiProvider>>();
            
            // Setup configuration values required by VertexAiProvider constructor
            _mockConfiguration.Setup(c => c["VertexAI:ProjectId"]).Returns("mock-project-id");
            _mockConfiguration.Setup(c => c["VertexAI:Location"]).Returns("us-central1");
            _mockConfiguration.Setup(c => c["VertexAI:KeyFilePath"]).Returns("mock-key-file-path");
        }

        [Fact]
        public void CreateVertexAiCompletionProvider_ReturnsNonNullDelegate()
        {
            // Act
            var completionDelegate = VertexAiProviderFactory.CreateVertexAiCompletionProvider(
                _mockConfiguration.Object,
                _mockLogger.Object,
                _testModelId);

            // Assert
            completionDelegate.Should().NotBeNull();
        }

        [Fact]
        public async Task InvokedDelegate_CallsGetCompletionAsyncWithCorrectModelId()
        {
            // Arrange
            // Create a testable provider that tracks calls to GetCompletionAsync
            var testProvider = new TestableVertexAiProvider(
                _mockConfiguration.Object,
                _mockLogger.Object);
            
            // Setup a delegate that uses our testable provider
            var completionDelegate = async (string systemPrompt, List<ChatMessageDto> history) =>
                await testProvider.GetCompletionAsync(systemPrompt, history, _testModelId);

            // Act
            await completionDelegate(_testSystemPrompt, _testHistory);

            // Assert
            testProvider.LastUsedModelId.Should().Be(_testModelId);
            testProvider.LastSystemPrompt.Should().Be(_testSystemPrompt);
            testProvider.LastHistory.Should().BeSameAs(_testHistory);
            testProvider.GetCompletionAsyncCallCount.Should().Be(1);
        }

        [Fact]
        public async Task InvokedDelegate_WithDifferentModelId_CallsGetCompletionAsyncWithThatModelId()
        {
            // Arrange
            // Although we only have one Vertex AI model defined so far, this test is for future expansion
            var customModelId = "custom-vertex-model";
            var testProvider = new TestableVertexAiProvider(
                _mockConfiguration.Object,
                _mockLogger.Object);
            
            // Setup a delegate that uses our testable provider
            var completionDelegate = async (string systemPrompt, List<ChatMessageDto> history) =>
                await testProvider.GetCompletionAsync(systemPrompt, history, customModelId);

            // Act
            await completionDelegate(_testSystemPrompt, _testHistory);

            // Assert
            testProvider.LastUsedModelId.Should().Be(customModelId);
            testProvider.GetCompletionAsyncCallCount.Should().Be(1);
        }

        // Test helper class to track calls to GetCompletionAsync
        private class TestableVertexAiProvider : VertexAiProvider
        {
            public string LastUsedModelId { get; private set; }
            public string LastSystemPrompt { get; private set; }
            public List<ChatMessageDto> LastHistory { get; private set; }
            public int GetCompletionAsyncCallCount { get; private set; }

            public TestableVertexAiProvider(
                IConfiguration configuration,
                ILogger<VertexAiProvider> logger)
                : base(configuration, logger)
            {
                GetCompletionAsyncCallCount = 0;
            }

            public override async Task<string?> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string modelId)
            {
                LastUsedModelId = modelId;
                LastSystemPrompt = systemPrompt;
                LastHistory = history;
                GetCompletionAsyncCallCount++;

                // Return a mock response
                return "Mock response from " + modelId;
            }
        }
    }
}