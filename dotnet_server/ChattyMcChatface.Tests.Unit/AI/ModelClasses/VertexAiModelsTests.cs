using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Services.AI.Vertex;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChattyMcChatface.Tests.Unit.AI.ModelClasses
{
    public class VertexAiModelsTests
    {
        [Fact]
        public void Constructor_ShouldInitializeAllModelFunctions()
        {
            // Arrange
            var mockConfiguration = new Mock<IConfiguration>();
            var mockVertexAiSection = new Mock<IConfigurationSection>();
            
            // Set up configuration for Vertex AI (Google Cloud)
            mockVertexAiSection.Setup(c => c.Value).Returns("mock-project-id");
            mockConfiguration.Setup(c => c["VertexAI:ProjectId"]).Returns("mock-project-id");
            mockConfiguration.Setup(c => c["VertexAI:Location"]).Returns("mock-location");
            mockConfiguration.Setup(c => c["VertexAI:KeyFilePath"]).Returns("mock-key-file-path");
            mockConfiguration.Setup(c => c.GetSection("VertexAI")).Returns(mockVertexAiSection.Object);
            
            var mockLogger = new Mock<ILogger<VertexAiProvider>>();
            var mockServiceProvider = new Mock<IServiceProvider>();

            // Set up the service provider to return the mocked dependencies
            // We're mocking the GetService method which is used internally by GetRequiredService extension method
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IConfiguration)))
                .Returns(mockConfiguration.Object);
            
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(ILogger<VertexAiProvider>)))
                .Returns(mockLogger.Object);

            // Act
            var vertexAiModels = new VertexAiModels(mockServiceProvider.Object);

            // Assert
            vertexAiModels.Claude37SonnetVertex.Should().NotBeNull();
        }
    }
}