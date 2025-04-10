using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ChattyMcChatface.Core.Services;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Data.Entities;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;
namespace ChattyMcChatface.Tests.Integration
{
    public class PersonaServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        private int _chatroomId;
        private int _testUserId;
        private int _personaUserId;
        private System.DateTime _lastUserMessageDate;
        private System.DateTime _lastPersonaMessageDate;

        public PersonaServiceIntegrationTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task PersonaRespondsToNewMessage_Scenario1()
        {
            var client = _fixture.CreateClientWithAuth();
            var mockNotificationService = _fixture.MockNotificationService;
            var scopeFactory = _fixture.Services.GetRequiredService<IServiceScopeFactory>();

            await _fixture.ResetDatabaseAsync(scopeFactory);

            _testUserId = 1;
            _personaUserId = 1001;

            var testUser = await _fixture.SeedUserAsync("Test", "User", "testuser@example.com", false, scopeFactory, _testUserId);
            var personaUser = await _fixture.SeedUserAsync("Persona", "Bot", "personauser@example.com", true, scopeFactory, _personaUserId);

            var chatroom = await _fixture.SeedChatroomAsync(new List<User> { testUser, personaUser }, _personaUserId, title: "Test Chatroom", scopeFactory);
            _chatroomId = chatroom.Id;

            var startDate = System.DateTime.UtcNow.AddHours(-1);

            await _fixture.SeedMessagesAsync(_chatroomId, 11, startDate, scopeFactory);
            await _fixture.SeedMessagesAsync(_chatroomId, 10, startDate.AddMinutes(1), scopeFactory);

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ChattyMcChatface.Data.AppDbContext>();

                var lastTwoMessages = await db.ChatMessages
                    .Where(m => m.ChatroomId == _chatroomId)
                    .OrderByDescending(m => m.Date)
                    .Take(2)
                    .ToListAsync();

                var lastUserMsg = lastTwoMessages.FirstOrDefault(m => m.UserId == _testUserId);
                var lastPersonaMsg = lastTwoMessages.FirstOrDefault(m => m.UserId == _personaUserId);

                _lastUserMessageDate = lastUserMsg?.Date ?? startDate;
                _lastPersonaMessageDate = lastPersonaMsg?.Date ?? startDate.AddMinutes(1);
            }

            await _fixture.SeedLastReadAsync(_chatroomId, _personaUserId, _lastPersonaMessageDate, scopeFactory);
            await _fixture.SeedLastReadAsync(_chatroomId, _testUserId, _lastUserMessageDate, scopeFactory);
            
            var newUserMessageDto = new ChatMessageDto
            {
                UserId = _testUserId,
                ChatroomId = _chatroomId,
                Text = "Hello Persona!",
                Role = MessageRole.User,
                UserFirstName = testUser.FirstName,
                UserLastName = testUser.LastName
            };

            var jsonContent = JsonContent.Create(newUserMessageDto);

            var response = await client.PostAsync($"/api/chatrooms/{_chatroomId}/chats", jsonContent);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

            await Task.Delay(TimeSpan.FromSeconds(10));

            ChattyMcChatface.Data.Entities.ChatMessage? personaResponseMessage = null;

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ChattyMcChatface.Data.AppDbContext>();

                personaResponseMessage = await db.ChatMessages
                    .Where(m => m.ChatroomId == _chatroomId
                             && m.UserId == _personaUserId
                             && m.Date > _lastUserMessageDate)
                    .OrderByDescending(m => m.Date)
                    .FirstOrDefaultAsync();
            }

            personaResponseMessage.Should().NotBeNull("because the persona should have responded.");
            personaResponseMessage.Text.Should().NotBeNullOrEmpty("because the persona response should contain text.");

            ChattyMcChatface.Data.Entities.LastRead? lastReadRecord = null;

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ChattyMcChatface.Data.AppDbContext>();

                lastReadRecord = await db.LastReads
                    .FirstOrDefaultAsync(lr => lr.ChatroomId == _chatroomId && lr.UserId == _personaUserId);
            }

            lastReadRecord.Should().NotBeNull("because a LastRead record should exist for the persona.");
            lastReadRecord.LastReadDate.Should().Be(personaResponseMessage.Date, "because the LastRead time should be updated to the timestamp of the persona's latest message.");

            mockNotificationService.Verify(
                x => x.SendMessageToGroupAsync(
                    _chatroomId.ToString(),
                    _chatroomId,
                    It.Is<ChatMessageDto>(dto =>
                        dto.Id == personaResponseMessage.Id &&
                        dto.Text == personaResponseMessage.Text &&
                        dto.Date == personaResponseMessage.Date &&
                        dto.UserId == _personaUserId &&
                        dto.ChatroomId == _chatroomId &&
                        dto.Role == MessageRole.Assistant
                    )
                ),
                Times.Once,
                "The notification service should have been called once with the correct persona message details."
            );
        }
    }
}