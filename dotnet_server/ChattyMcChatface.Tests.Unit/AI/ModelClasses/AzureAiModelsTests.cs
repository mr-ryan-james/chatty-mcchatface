using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Services.AI.Azure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChattyMcChatface.Tests.Unit.AI.ModelClasses
{
    public class AzureAiModelsTests
    {
        [Fact]
        public void Constructor_ShouldInitializeAllModelFunctions()
        {
            // Arrange
            var mockConfiguration = new Mock<IConfiguration>();
            var mockConfigSection = new Mock<IConfigurationSection>();
            
            // Set up configuration to return mock Azure OpenAI settings
            mockConfigSection.Setup(c => c.Value).Returns("mock-api-key");
            mockConfiguration.Setup(c => c["AzureOpenAI:ApiKey"]).Returns("mock-api-key");
            mockConfiguration.Setup(c => c["AzureOpenAI:Endpoint"]).Returns("https://mock-endpoint.openai.azure.com");
            mockConfiguration.Setup(c => c.GetSection("AzureOpenAI")).Returns(mockConfigSection.Object);
            
            var mockLogger = new Mock<ILogger<AzureAiProvider>>();
            var mockServiceProvider = new Mock<IServiceProvider>();

            // Set up the service provider to return the mocked dependencies
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IConfiguration)))
                .Returns(mockConfiguration.Object);
            
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(ILogger<AzureAiProvider>)))
                .Returns(mockLogger.Object);

            // Act
            var azureAiModels = new AzureAiModels(mockServiceProvider.Object);

            // Assert
            azureAiModels.Gpt4oThrivify.Should().NotBeNull();
            azureAiModels.Gpt45PreviewRyan.Should().NotBeNull();
        }
    }
}