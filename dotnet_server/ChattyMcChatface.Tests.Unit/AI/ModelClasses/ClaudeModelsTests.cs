using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Services.AI.Claude;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChattyMcChatface.Tests.Unit.AI.ModelClasses
{
    public class ClaudeModelsTests
    {
        [Fact]
        public void Constructor_ShouldInitializeAllModelFunctions()
        {
            // Arrange
            var mockConfiguration = new Mock<IConfiguration>();
            var mockConfigSection = new Mock<IConfigurationSection>();
            
            // Set up configuration to return a mock API key
            mockConfigSection.Setup(c => c.Value).Returns("mock-api-key");
            mockConfiguration.Setup(c => c["Anthropic:ApiKey"]).Returns("mock-api-key");
            mockConfiguration.Setup(c => c.GetSection("Anthropic")).Returns(mockConfigSection.Object);
            
            var mockLogger = new Mock<ILogger<ClaudeProvider>>();
            var mockServiceProvider = new Mock<IServiceProvider>();

            // Set up the service provider to return the mocked dependencies
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IConfiguration)))
                .Returns(mockConfiguration.Object);
            
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(ILogger<ClaudeProvider>)))
                .Returns(mockLogger.Object);

            // Act
            var claudeModels = new ClaudeModels(mockServiceProvider.Object);

            // Assert
            claudeModels.Claude37Sonnet.Should().NotBeNull();
            claudeModels.ClaudeInstant.Should().NotBeNull();
        }
    }
}