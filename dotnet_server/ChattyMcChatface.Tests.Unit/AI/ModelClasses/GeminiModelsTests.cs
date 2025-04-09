using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Services.AI.Gemini;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChattyMcChatface.Tests.Unit.AI.ModelClasses
{
    public class GeminiModelsTests
    {
        [Fact]
        public void Constructor_ShouldInitializeAllModelFunctions()
        {
            // Arrange
            var mockConfiguration = new Mock<IConfiguration>();
            var mockConfigSection = new Mock<IConfigurationSection>();
            
            // Set up configuration to return a mock API key
            mockConfigSection.Setup(c => c.Value).Returns("mock-api-key");
            mockConfiguration.Setup(c => c["Gemini:ApiKey"]).Returns("mock-api-key");
            mockConfiguration.Setup(c => c.GetSection("Gemini")).Returns(mockConfigSection.Object);
            
            var mockLogger = new Mock<ILogger<GeminiProvider>>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            var mockServiceProvider = new Mock<IServiceProvider>();

            // Set up the HttpClientFactory to return a default HttpClient
            mockHttpClientFactory
                .Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient());

            // Set up the service provider to return the mocked dependencies
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IConfiguration)))
                .Returns(mockConfiguration.Object);
            
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(ILogger<GeminiProvider>)))
                .Returns(mockLogger.Object);
                
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IHttpClientFactory)))
                .Returns(mockHttpClientFactory.Object);

            // Act
            var geminiModels = new GeminiModels(mockServiceProvider.Object);

            // Assert
            geminiModels.Gemini20Flash.Should().NotBeNull();
            geminiModels.Gemini25Pro.Should().NotBeNull();
        }
    }
}