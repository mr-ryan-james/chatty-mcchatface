using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ChattyMcChatface.Core.Services;
using ChattyMcChatface.Core.Dtos;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ChattyMcChatface.Tests.Integration
{
    public class PersonaServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;
        private readonly IPersonaService _personaService;
        private readonly ILogger<PersonaService> _logger;
        private readonly IConfiguration _config;

        public PersonaServiceIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _config = _fixture.Services.GetRequiredService<IConfiguration>();
            _logger = _fixture.Services.GetRequiredService<ILogger<PersonaService>>();
            _personaService = _fixture.Services.GetRequiredService<IPersonaService>();
        }

        [Fact(Skip = "Requires secrets configuration and live API calls")]
        public async Task GenerateResponseAsync_WithValidInput_ReturnsResponse()
        {
            // Arrange
            var chatroomId = 1; // Test chatroom ID
            var triggeringMessage = new ChatMessageDto
            {
                Text = "Tell me about yourself.",
                UserId = 1,
                UserFirstName = "Test",
                UserLastName = "User",
                ChatroomId = chatroomId,
                Role = MessageRole.User
            };

            // Act
            await _personaService.GenerateResponseAsync(chatroomId, triggeringMessage);

            // Assert
            // Since this is an integration test, we would typically verify the response was saved to the DB
            // For now this is just a placeholder test that verifies the method doesn't throw an exception
        }
    }
}