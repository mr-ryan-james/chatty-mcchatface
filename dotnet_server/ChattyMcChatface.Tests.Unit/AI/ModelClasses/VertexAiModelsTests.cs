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
using System.Net.Http;
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
            var mockVertexSection = new Mock<IConfigurationSection>();
            // Set up configuration for Vertex AI (Google Cloud)
            mockConfiguration.Setup(c => c["VertexAI:Location"]).Returns("mock-region");
            // Add mock JSON content for the service account key (required by VertexAiProvider)
            mockConfiguration.Setup(c => c["VertexAI:KeyJsonContent"]).Returns(@"{
                ""type"": ""service_account"",
                ""project_id"": ""mock-project-id"",
                ""private_key_id"": ""mock-key-id"",
                ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMOCKKEY\n-----END PRIVATE KEY-----\n"",
                ""client_email"": ""mock@example.iam.gserviceaccount.com"",
                ""client_id"": ""123456789"",
                ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                ""token_uri"": ""https://oauth2.googleapis.com/token"",
                ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
                ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/mock@example.iam.gserviceaccount.com""
            }");
            // Setup the configuration section
            mockConfiguration.Setup(c => c.GetSection("Vertex")).Returns(mockVertexSection.Object);
            
            var mockLogger = new Mock<ILogger<VertexAiProvider>>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            // Ensure the service provider is non-null
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);

            // Set up the service provider to return the mocked dependencies
            // We're mocking the GetService method which is used internally by GetRequiredService extension method
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IConfiguration)))
                .Returns(mockConfiguration.Object);
            
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(ILogger<VertexAiProvider>)))
                .Returns(mockLogger.Object);
                
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IHttpClientFactory)))
                .Returns(mockHttpClientFactory.Object);

            // Act
            var vertexAiModels = new VertexAiModels(mockServiceProvider.Object);

            // Assert
            vertexAiModels.Claude37SonnetVertex.Should().NotBeNull();
        }
    }
}