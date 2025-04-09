using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Services.AI.Gemini;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChattyMcChatface.Tests.Unit.AI.Factories
{
    public class GeminiProviderFactoryTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<GeminiProvider>> _mockLogger;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly string _testModelId = AiModels.Gemini20Flash;
        private readonly string _testSystemPrompt = "Test system prompt";
        private readonly List<ChatMessageDto> _testHistory = new List<ChatMessageDto>();

        public GeminiProviderFactoryTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<GeminiProvider>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            
            // Setup a mock for the Gemini API key
            _mockConfiguration.Setup(c => c["Gemini:ApiKey"]).Returns("mock-gemini-api-key");
            
            // Setup HttpClientFactory to return a mock HttpClient
            var mockHttpClient = new Mock<HttpClient>();
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient());
        }

        [Fact]
        public void CreateGeminiCompletionProvider_ReturnsNonNullDelegate()
        {
            // Act
            var completionDelegate = GeminiProviderFactory.CreateGeminiCompletionProvider(
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockHttpClientFactory.Object,
                _testModelId);

            // Assert
            completionDelegate.Should().NotBeNull();
        }

        [Fact]
        public async Task InvokedDelegate_CallsGetCompletionAsyncWithCorrectModelId()
        {
            // Arrange
            // Create a testable provider that tracks calls to GetCompletionAsync
            var testProvider = new TestableGeminiProvider(
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockHttpClientFactory.Object);
            
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
            var customModelId = AiModels.Gemini25Pro;
            var testProvider = new TestableGeminiProvider(
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockHttpClientFactory.Object);
            
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
        private class TestableGeminiProvider : GeminiProvider
        {
            public string LastUsedModelId { get; private set; }
            public string LastSystemPrompt { get; private set; }
            public List<ChatMessageDto> LastHistory { get; private set; }
            public int GetCompletionAsyncCallCount { get; private set; }

            public TestableGeminiProvider(
                IConfiguration configuration,
                ILogger<GeminiProvider> logger,
                IHttpClientFactory httpClientFactory)
                : base(configuration, logger, httpClientFactory)
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