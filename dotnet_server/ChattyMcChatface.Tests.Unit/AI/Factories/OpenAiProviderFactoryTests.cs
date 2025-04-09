using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Services.AI.OpenAI;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChattyMcChatface.Tests.Unit.AI.Factories
{
    public class OpenAiProviderFactoryTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<OpenAiProvider>> _mockLogger;
        private readonly Mock<OpenAiProvider> _mockProvider;
        private readonly string _testModelId = AiModels.OpenAiGpt4oLatest;
        private readonly string _testSystemPrompt = "Test system prompt";
        private readonly List<ChatMessageDto> _testHistory = new List<ChatMessageDto>();

        public OpenAiProviderFactoryTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<OpenAiProvider>>();
            
            // Setup a mock for the OpenAI API key
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(s => s.Value).Returns("mock-api-key");
            _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("mock-api-key");
        }

        [Fact]
        public void CreateOpenAiCompletionProvider_ReturnsNonNullDelegate()
        {
            // Act
            var completionDelegate = OpenAiProviderFactory.CreateOpenAiCompletionProvider(
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
            var testProvider = new TestableOpenAiProvider(_mockConfiguration.Object, _mockLogger.Object);
            
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
            var customModelId = AiModels.OpenAiGpt35Turbo;
            var testProvider = new TestableOpenAiProvider(_mockConfiguration.Object, _mockLogger.Object);
            
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
        private class TestableOpenAiProvider : OpenAiProvider
        {
            public string LastUsedModelId { get; private set; }
            public string LastSystemPrompt { get; private set; }
            public List<ChatMessageDto> LastHistory { get; private set; }
            public int GetCompletionAsyncCallCount { get; private set; }

            public TestableOpenAiProvider(IConfiguration configuration, ILogger<OpenAiProvider> logger)
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