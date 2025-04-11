using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

// Assuming PersonaInfo record is accessible or defined similarly here
// If not, you might need to define it or adjust the deserialization target.
// public record PersonaInfo(int Id, string DisplayName, string? SystemPrompt, string? PreferredModelId);

namespace ChattyMcChatface.Tests.Integration
{
    public class PersonasControllerIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        public PersonasControllerIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetPersonas_ReturnsSeededPersonas()
        {
            // Arrange
            // The fixture applies migrations, which now includes seeding personas 1001, 1002, 1003
            var client = _fixture.CreateClientWithAuth(); // Use authenticated client

            // Act
            var response = await client.GetAsync("/api/personas");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var personas = await response.Content.ReadFromJsonAsync<List<PersonaInfo>>(); // Use the correct record/DTO

            Assert.NotNull(personas);
            Assert.Equal(3, personas.Count); // Expecting the 3 seeded personas

            // Verify specific personas exist (optional but good)
            Assert.Contains(personas, p => p.Id == 1001 && p.DisplayName == "Persona 1001");
            Assert.Contains(personas, p => p.Id == 1002 && p.DisplayName == "Persona 1002");
            Assert.Contains(personas, p => p.Id == 1003 && p.DisplayName == "Persona 1003");
            Assert.All(personas, p => Assert.True(p.Id >= 1001 && p.Id <= 1003)); // Ensure only expected IDs
        }

        [Fact]
        public async Task PostPersonas_ReturnsMethodNotAllowed()
        {
            // Arrange
            var client = _fixture.CreateClientWithAuth(); // Use authenticated client
            var dummyPayload = new { name = "Invalid", persona_prompt = "Should not work" }; // Content doesn't matter much

            // Act
            var response = await client.PostAsJsonAsync("/api/personas", dummyPayload);

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }
    }
}

// Define PersonaInfo here if it's not accessible otherwise
// public record PersonaInfo(int Id, string DisplayName, string? SystemPrompt, string? PreferredModelId);