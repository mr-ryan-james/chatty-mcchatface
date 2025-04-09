using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Services.AI.Gemini;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
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
            // No longer need IServiceProvider mock for GeminiModels constructor

            // Set up the HttpClientFactory to return a default HttpClient (using a mock handler)
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            
            // Setup the message handler using Moq.Protected for HttpMessageHandler
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Moq.Protected.ItExpr.IsAny<HttpRequestMessage>(),
                    Moq.Protected.ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent("{\"candidates\": [{\"content\": {\"parts\": [{\"text\": \"test response\"}]}}]}")
                });
                
            mockHttpClientFactory
                .Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(mockHttpMessageHandler.Object));

            // Act
            var geminiModels = new GeminiModels(mockConfiguration.Object, mockLogger.Object, mockHttpClientFactory.Object);

            // Assert
            geminiModels.Gemini20Flash.Should().NotBeNull();
            geminiModels.Gemini25Pro.Should().NotBeNull();
        }
    }
}