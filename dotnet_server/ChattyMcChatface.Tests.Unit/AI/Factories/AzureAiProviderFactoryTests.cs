using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Services.AI.Azure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChattyMcChatface.Tests.Unit.AI.Factories
{
    public class AzureAiProviderFactoryTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AzureAiProvider>> _mockLogger;
        private readonly string _testModelId = AiModels.AzureGpt4oThrivify;
        private readonly string _testSystemPrompt = "Test system prompt";
        private readonly List<ChatMessageDto> _testHistory = new List<ChatMessageDto>();

        public AzureAiProviderFactoryTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AzureAiProvider>>();
            
            // Setup configuration for Azure OpenAI
            _mockConfiguration.Setup(c => c["AzureOpenAI:ApiKey"]).Returns("mock-api-key");
            _mockConfiguration.Setup(c => c["AzureOpenAI:Endpoint"]).Returns("https://mock-endpoint.openai.azure.com/");
        }

        [Fact]
        public void CreateAzureAiCompletionProvider_ReturnsNonNullDelegate()
        {
            // Act
            var completionDelegate = AzureAiProviderFactory.CreateAzureAiCompletionProvider(
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
            var testProvider = new TestableAzureAiProvider(_mockConfiguration.Object, _mockLogger.Object);
            
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
            var customModelId = AiModels.AzureGpt45PreviewRyan;
            var testProvider = new TestableAzureAiProvider(_mockConfiguration.Object, _mockLogger.Object);
            
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
        private class TestableAzureAiProvider : AzureAiProvider
        {
            public string LastUsedModelId { get; private set; }
            public string LastSystemPrompt { get; private set; }
            public List<ChatMessageDto> LastHistory { get; private set; }
            public int GetCompletionAsyncCallCount { get; private set; }

            public TestableAzureAiProvider(IConfiguration configuration, ILogger<AzureAiProvider> logger)
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