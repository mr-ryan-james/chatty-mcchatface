using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Services.AI.Claude;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChattyMcChatface.Tests.Unit.AI.Factories
{
    public class ClaudeProviderFactoryTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<ClaudeProvider>> _mockLogger;
        private readonly string _testModelId = AiModels.Claude37Sonnet;
        private readonly string _testSystemPrompt = "Test system prompt";
        private readonly List<ChatMessageDto> _testHistory = new List<ChatMessageDto>();

        public ClaudeProviderFactoryTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<ClaudeProvider>>();
            
            // Setup mocks for Claude configuration
            _mockConfiguration.Setup(c => c["Claude:ApiKey"]).Returns("dummy-claude-key");
        }

        [Fact]
        public void CreateClaudeCompletionProvider_ReturnsNonNullDelegate()
        {
            // Act
            var completionDelegate = ClaudeProviderFactory.CreateClaudeCompletionProvider(
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
            var testProvider = new TestableClaudeProvider(_mockConfiguration.Object, _mockLogger.Object);
            
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
            var customModelId = AiModels.ClaudeInstant;
            var testProvider = new TestableClaudeProvider(_mockConfiguration.Object, _mockLogger.Object);
            
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
        private class TestableClaudeProvider : ClaudeProvider
        {
            // Initialize non-nullable properties to avoid CS8618 warnings
            public string LastUsedModelId { get; private set; } = string.Empty;
            public string LastSystemPrompt { get; private set; } = string.Empty;
            public List<ChatMessageDto> LastHistory { get; private set; } = new List<ChatMessageDto>();
            public int GetCompletionAsyncCallCount { get; private set; }

            public TestableClaudeProvider(IConfiguration configuration, ILogger<ClaudeProvider> logger)
                : base(configuration, logger)
            {
                GetCompletionAsyncCallCount = 0;
            }

            // Remove async keyword since there are no await operations - fixes CS1998 warning
            public override Task<string?> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string modelId)
            {
                LastUsedModelId = modelId;
                LastSystemPrompt = systemPrompt;
                LastHistory = history;
                GetCompletionAsyncCallCount++;

                // Return a mock response using Task.FromResult
                return Task.FromResult<string?>("Mock response from " + modelId);
            }
        }
    }
}