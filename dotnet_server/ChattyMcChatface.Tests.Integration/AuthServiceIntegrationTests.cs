using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ChattyMcChatface.Tests.Integration
{
    public class AuthServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;
        private readonly HttpClient _client;

        public AuthServiceIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _client = fixture.CreateClient(); // Use fixture to create client
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_CreatesUserAndReturnsToken()
        {
            // Arrange
            // Ensure a unique email for each test run
            var uniqueEmail = $"testuser_{System.Guid.NewGuid()}@example.com";
            var registerDto = new UserRegisterDto
            {
                Email = uniqueEmail,
                Password = "password123",
                FirstName = "Test",
                LastName = "User",
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 2xx
            Assert.Equal(HttpStatusCode.OK, response.StatusCode); // Or Created (201) depending on API design

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(authResponse);
            Assert.NotEmpty(authResponse.Token);
            Assert.Equal(registerDto.Email, authResponse.Email);
            Assert.True(authResponse.UserId > 0);

            // Verify user exists in DB and schema is correct (implicitly by accessing properties)
            using var scope = _fixture.Services.CreateScope(); // Create a scope to resolve DbContext
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var createdUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == uniqueEmail);

            Assert.NotNull(createdUser);
            Assert.Equal(registerDto.FirstName, createdUser.FirstName);
            Assert.Equal(registerDto.LastName, createdUser.LastName);
            Assert.Equal(authResponse.UserId, createdUser.Id);

            // Implicitly test schema by accessing potentially missing columns
            // If these columns don't exist in the test DB schema, accessing them would throw an error
            _ = createdUser.PreferredModelId;
            _ = createdUser.SystemPrompt;
            _ = createdUser.IsPersona; // Accessing another potentially added column

            // Optional: Clean up the created user if necessary, though test databases are often ephemeral
            // dbContext.Users.Remove(createdUser);
            // await dbContext.SaveChangesAsync();
        }

                // Add more tests for login, edge cases, etc. later
        
                [Fact]
                public async Task LoginAsync_WithValidCredentials_ReturnsToken()
                {
                    // Arrange - Register user first to ensure correct password hash
                    var uniqueEmail = $"login_success_{System.Guid.NewGuid()}@example.com";
                    var password = "password123";
                    var registerDto = new UserRegisterDto
                    {
                        Email = uniqueEmail, Password = password, FirstName = "Login", LastName = "Test"
                    };
                    var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
                    registerResponse.EnsureSuccessStatusCode(); // Ensure registration succeeded
                    var registeredUser = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
                    Assert.NotNull(registeredUser);

                    // Prepare login DTO
                    var loginDto = new UserLoginDto { Email = uniqueEmail, Password = password };

                    // Act
                    var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

                    // Assert
                    response.EnsureSuccessStatusCode();
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                    Assert.NotNull(authResponse);
                    Assert.NotEmpty(authResponse.Token);
                    Assert.Equal(registeredUser.Email, authResponse.Email);
                    Assert.Equal(registeredUser.UserId, authResponse.UserId);
                }
        
                [Fact]
                public async Task LoginAsync_WithInvalidCredentials_ReturnsBadRequest()
                {
                    // Arrange
                    var uniqueEmail = $"login_fail_{System.Guid.NewGuid()}@example.com";
                    var registerDto = new UserRegisterDto
                    {
                        Email = uniqueEmail, Password = "correctpassword", FirstName = "LoginFail", LastName = "Test"
                    };
                    var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
                    registerResponse.EnsureSuccessStatusCode(); // Ensure registration succeeded

                    // Prepare login DTO with wrong password
                    var loginDto = new UserLoginDto { Email = uniqueEmail, Password = "wrongpassword" };

                    // Act
                    var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); // API returns 401 for invalid credentials
                }
        
                [Fact]
                public async Task LoginAsync_WithNonExistentUser_ReturnsBadRequest()
                {
                    // Arrange
                    var loginDto = new UserLoginDto { Email = "nosuchuser@example.com", Password = "password" };
        
                    // Act
                    var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        
                    // Assert
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); // API returns 401 for non-existent user
                }
            }
}