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
        // Remove unused field that was causing warning CS0414
        private readonly List<ChatMessageDto> _testHistory = new List<ChatMessageDto>();

        public AzureAiProviderFactoryTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AzureAiProvider>>();
        }

        #region Thrivify Provider Tests

        [Fact]
        public void CreateAzureThrivifyCompletionProvider_WithValidConfig_ReturnsNonNullDelegate()
        {
            // Arrange
            SetupThrivifyConfigurationMock("fake-thrivify-key", "https://fake-thrivify.openai.azure.com/");

            // Act
            var completionDelegate = AzureAiProviderFactory.CreateAzureThrivifyCompletionProvider(
                _mockConfiguration.Object,
                _mockLogger.Object,
                _testModelId);

            // Assert
            completionDelegate.Should().NotBeNull();
        }

        [Fact]
        public void CreateAzureThrivifyCompletionProvider_WithMissingApiKey_ThrowsInvalidOperationException()
        {
            // Arrange
            SetupThrivifyConfigurationMock(null, "https://fake-thrivify.openai.azure.com/");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                AzureAiProviderFactory.CreateAzureThrivifyCompletionProvider(
                    _mockConfiguration.Object,
                    _mockLogger.Object,
                    _testModelId));

            exception.Message.Should().Contain("AzureOpenAI:Thrivify:ApiKey");
        }

        [Fact]
        public void CreateAzureThrivifyCompletionProvider_WithMissingEndpoint_ThrowsInvalidOperationException()
        {
            // Arrange - Set API key but missing endpoint
            _mockConfiguration.Setup(c => c["AzureOpenAI:Thrivify:ApiKey"]).Returns("fake-api-key");
            // Use string.Empty instead of null to avoid CS8600 warning
            _mockConfiguration.Setup(c => c["AzureOpenAI:Thrivify:Endpoint"]).Returns(string.Empty);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                AzureAiProviderFactory.CreateAzureThrivifyCompletionProvider(
                    _mockConfiguration.Object,
                    _mockLogger.Object,
                    _testModelId));

            exception.Message.Should().Contain("AzureOpenAI:Thrivify:Endpoint");
        }

        #endregion

        #region Ryan Provider Tests

        [Fact]
        public void CreateAzureRyanCompletionProvider_WithValidConfig_ReturnsNonNullDelegate()
        {
            // Arrange
            SetupRyanConfigurationMock("fake-ryan-key", "https://fake-ryan.openai.azure.com/");

            // Act
            var completionDelegate = AzureAiProviderFactory.CreateAzureRyanCompletionProvider(
                _mockConfiguration.Object,
                _mockLogger.Object,
                _testModelId);

            // Assert
            completionDelegate.Should().NotBeNull();
        }

        [Fact]
        public void CreateAzureRyanCompletionProvider_WithMissingApiKey_ThrowsInvalidOperationException()
        {
            // Arrange
            SetupRyanConfigurationMock(null, "https://fake-ryan.openai.azure.com/");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                AzureAiProviderFactory.CreateAzureRyanCompletionProvider(
                    _mockConfiguration.Object,
                    _mockLogger.Object,
                    _testModelId));

            exception.Message.Should().Contain("AzureOpenAI:Ryan:ApiKey");
        }

        [Fact]
        public void CreateAzureRyanCompletionProvider_WithMissingEndpoint_ThrowsInvalidOperationException()
        {
            // Arrange - Set API key but missing endpoint
            _mockConfiguration.Setup(c => c["AzureOpenAI:Ryan:ApiKey"]).Returns("fake-api-key");
            // Use string.Empty instead of null to avoid CS8600 warning
            _mockConfiguration.Setup(c => c["AzureOpenAI:Ryan:Endpoint"]).Returns(string.Empty);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                AzureAiProviderFactory.CreateAzureRyanCompletionProvider(
                    _mockConfiguration.Object,
                    _mockLogger.Object,
                    _testModelId));

            exception.Message.Should().Contain("AzureOpenAI:Ryan:Endpoint");
        }

        #endregion

        #region Helper Methods

        private void SetupThrivifyConfigurationMock(string? apiKey, string? endpoint)
        {
            // Setup configuration for Thrivify using indexer access
            _mockConfiguration.Setup(c => c["AzureOpenAI:Thrivify:ApiKey"]).Returns(apiKey);
            _mockConfiguration.Setup(c => c["AzureOpenAI:Thrivify:Endpoint"]).Returns(endpoint);
        }

        private void SetupRyanConfigurationMock(string? apiKey, string? endpoint)
        {
            // Setup configuration for Ryan using indexer access
            _mockConfiguration.Setup(c => c["AzureOpenAI:Ryan:ApiKey"]).Returns(apiKey);
            _mockConfiguration.Setup(c => c["AzureOpenAI:Ryan:Endpoint"]).Returns(endpoint);
        }

        #endregion
    }
}