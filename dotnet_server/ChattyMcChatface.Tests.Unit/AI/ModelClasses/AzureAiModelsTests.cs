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
            // Setup specific config values for each required endpoint
            mockConfigSection.Setup(c => c.Value).Returns("mock-api-key");
            
            // Thrivify endpoint settings
            mockConfiguration.Setup(c => c["AzureOpenAI:Thrivify:ApiKey"]).Returns("mock-api-key-thrivify");
            mockConfiguration.Setup(c => c["AzureOpenAI:Thrivify:Endpoint"]).Returns("https://mock-endpoint-thrivify.openai.azure.com");
            
            // Ryan endpoint settings
            mockConfiguration.Setup(c => c["AzureOpenAI:Ryan:ApiKey"]).Returns("mock-api-key-ryan");
            mockConfiguration.Setup(c => c["AzureOpenAI:Ryan:Endpoint"]).Returns("https://mock-endpoint-ryan.openai.azure.com");
            
            // Legacy configuration (in case it's still used)
            mockConfiguration.Setup(c => c["AzureOpenAI:ApiKey"]).Returns("mock-api-key");
            mockConfiguration.Setup(c => c["AzureOpenAI:Endpoint"]).Returns("https://mock-endpoint.openai.azure.com");
            
            mockConfiguration.Setup(c => c.GetSection("AzureOpenAI")).Returns(mockConfigSection.Object);
            mockConfiguration.Setup(c => c.GetSection("AzureOpenAI:Thrivify")).Returns(mockConfigSection.Object);
            mockConfiguration.Setup(c => c.GetSection("AzureOpenAI:Ryan")).Returns(mockConfigSection.Object);
            
            var mockLogger = new Mock<ILogger<AzureAiProvider>>();
            // Use strict mocking behavior to catch unintended service requests
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);

            // Set up the service provider to return the mocked dependencies
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IConfiguration)))
                .Returns(mockConfiguration.Object);
            
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(ILogger<AzureAiProvider>)))
                .Returns(mockLogger.Object);
                
            // Setup for unexpected service requests to ensure we get a clear error
            mockServiceProvider
                .Setup(sp => sp.GetService(It.Is<Type>(t =>
                    t != typeof(IConfiguration) &&
                    t != typeof(ILogger<AzureAiProvider>))))
                .Throws(new InvalidOperationException($"Unexpected service request in AzureAiModels test"));

            // Act
            var azureAiModels = new AzureAiModels(mockServiceProvider.Object);

            // Assert
            azureAiModels.Gpt4oThrivify.Should().NotBeNull();
            azureAiModels.Gpt45PreviewRyan.Should().NotBeNull();
        }
    }
}