using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos; // Assuming UserDto is here
using Microsoft.Extensions.DependencyInjection; // Add this using
using Xunit;

namespace ChattyMcChatface.Tests.Integration
{
    public class UsersControllerIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        public UsersControllerIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetUsers_ReturnsOtherRegularUsers_ExcludesSelfAndPersonas()
        {
            // Arrange
            // Seed necessary users using the fixture helper
            var scopeFactory = _fixture.Services.GetRequiredService<IServiceScopeFactory>(); // Get ScopeFactory
            // Note: Personas 1001, 1002, 1003 are seeded by the migration applied by the fixture
            var requestingUser = await _fixture.SeedUserAsync("Requesting", "User", $"req_{System.Guid.NewGuid()}@test.com", false, scopeFactory, id: 500); // Pass ScopeFactory
            var otherUser1 = await _fixture.SeedUserAsync("Other", "User1", $"other1_{System.Guid.NewGuid()}@test.com", false, scopeFactory); // Pass ScopeFactory
            var otherUser2 = await _fixture.SeedUserAsync("Other", "User2", $"other2_{System.Guid.NewGuid()}@test.com", false, scopeFactory); // Pass ScopeFactory
            // Persona 1001, 1002, 1003 should already exist due to migration

            // Create an authenticated client acting as 'requestingUser'
            var client = _fixture.CreateClientWithAuth(userId: requestingUser.Id.ToString());

            // Act
            var response = await client.GetAsync("/api/users");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();

            Assert.NotNull(users);
            // Verify the list contains the other regular users
            Assert.Contains(users, u => u.Id == otherUser1.Id);
            Assert.Contains(users, u => u.Id == otherUser2.Id);

            // Verify the list does NOT contain the requesting user
            Assert.DoesNotContain(users, u => u.Id == requestingUser.Id);

            // Verify the list does NOT contain the persona users
            Assert.DoesNotContain(users, u => u.Id == 1001);
            Assert.DoesNotContain(users, u => u.Id == 1002);
            Assert.DoesNotContain(users, u => u.Id == 1003);

            // Optional: Verify the exact count if no other users exist
            // Assert.Equal(2, users.Count);
        }
    }
}