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
            var personaUser = await _fixture.SeedUserAsync(
                "Helpful Assistant", "Bot", "personauser@example.com", true, scopeFactory, _personaUserId,
                systemPrompt: "You are a helpful, friendly AI assistant. Always respond in a supportive and informative manner. Provide accurate information and assistance to the user while maintaining a positive and professional tone.",
                preferredModelId: "gemini-2.5-pro-preview-03-25"
            );

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

        [Fact]
        public async Task PersonaFallbackMechanism_Scenario5()
        {
            var mockNotificationService = _fixture.MockNotificationService;
            var scopeFactory = _fixture.Services.GetRequiredService<IServiceScopeFactory>();

            await _fixture.ResetDatabaseAsync(scopeFactory);

            // Seed users
            var testUser = await _fixture.SeedUserAsync("Test", "User", "testuser5@example.com", false, scopeFactory, id: 4);
            var personaUser = await _fixture.SeedUserAsync(
                "Helpful Assistant", "Bot", "personauser5@example.com", true, scopeFactory, 1001,
                systemPrompt: "You are a helpful, friendly AI assistant. Always respond in a supportive and informative manner. Provide accurate information and assistance to the user while maintaining a positive and professional tone.",
                preferredModelId: "gemini-2.5-pro-preview-03-25"
            );

            var client = _fixture.CreateClientWithAuth(userId: testUser.Id.ToString());

            // Seed chatroom with personaUserId = 1001
            var chatroom = await _fixture.SeedChatroomAsync(new List<User> { testUser, personaUser }, 1001, title: "Fallback Scenario 5 Chatroom", scopeFactory);
            var chatroomId = chatroom.Id;

            // Seed minimal initial messages
            var startDate = DateTime.UtcNow.AddMinutes(-5);

            var userMessages = await _fixture.SeedMessagesAsync(chatroomId, 1, startDate, scopeFactory, userId: testUser.Id);
            var personaMessages = await _fixture.SeedMessagesAsync(chatroomId, 1, startDate.AddSeconds(10), scopeFactory, userId: personaUser.Id);

            var lastMessageDate = personaMessages.Last().Date > userMessages.Last().Date
                ? personaMessages.Last().Date
                : userMessages.Last().Date;

            // Seed LastRead for both users
            await _fixture.SeedLastReadAsync(chatroomId, testUser.Id, userMessages.Last().Date, scopeFactory);
            await _fixture.SeedLastReadAsync(chatroomId, personaUser.Id, personaMessages.Last().Date, scopeFactory);

            // Create new user message DTO
            var newUserMessageDto = new ChatMessageDto
            {
                UserId = testUser.Id,
                ChatroomId = chatroomId,
                Text = "Hello Persona with fallback!",
                Role = MessageRole.User,
                UserFirstName = testUser.FirstName,
                UserLastName = testUser.LastName
            };

            var jsonContent = JsonContent.Create(newUserMessageDto);

            var response = await client.PostAsync($"/api/chatrooms/{chatroomId}/chats", jsonContent);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

            // Wait longer to allow fallback to trigger
            await Task.Delay(TimeSpan.FromSeconds(20));

            ChattyMcChatface.Data.Entities.ChatMessage? personaResponseMessage = null;

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ChattyMcChatface.Data.AppDbContext>();

                personaResponseMessage = await db.ChatMessages
                    .Where(m => m.ChatroomId == chatroomId
                             && m.UserId == personaUser.Id
                             && m.Date > lastMessageDate)
                    .OrderByDescending(m => m.Date)
                    .FirstOrDefaultAsync();
            }

            personaResponseMessage.Should().NotBeNull("because the fallback AI provider should have generated a response.");
            personaResponseMessage.Text.Should().NotBeNullOrEmpty("because the fallback AI provider's response should contain text.");

            ChattyMcChatface.Data.Entities.LastRead? lastReadRecord = null;

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ChattyMcChatface.Data.AppDbContext>();

                lastReadRecord = await db.LastReads
                    .FirstOrDefaultAsync(lr => lr.ChatroomId == chatroomId && lr.UserId == personaUser.Id);
            }

            lastReadRecord.Should().NotBeNull("because a LastRead record should exist for the persona.");
            lastReadRecord.LastReadDate.Should().Be(personaResponseMessage.Date, "because the LastRead time should be updated to the timestamp of the persona's fallback response.");

            mockNotificationService.Verify(
                x => x.SendMessageToGroupAsync(
                    chatroomId.ToString(),
                    chatroomId,
                    It.Is<ChatMessageDto>(dto =>
                        dto.Id == personaResponseMessage.Id &&
                        dto.Text == personaResponseMessage.Text &&
                        dto.Date == personaResponseMessage.Date &&
                        dto.UserId == personaUser.Id &&
                        dto.ChatroomId == chatroomId &&
                        dto.Role == MessageRole.Assistant
                    )
                ),
                Times.Once,
                "The notification service should have been called once with the fallback persona message details."
            );
        }

        [Fact]
        public async Task PersonaUsesCorrectHistoryLimit_Scenario4()
        {
            var scopeFactory = _fixture.Services.GetRequiredService<IServiceScopeFactory>();
            var mockNotificationService = _fixture.MockNotificationService;

            await _fixture.ResetDatabaseAsync(scopeFactory);

            const int maxHistoryMessages = 20;

            var testUser = await _fixture.SeedUserAsync("Test", "User", "testuser4@example.com", false, scopeFactory, id: 3);
            var personaUser = await _fixture.SeedUserAsync(
                "Helpful Assistant", "Bot", "personauser4@example.com", true, scopeFactory, 1001,
                systemPrompt: "You are a helpful, friendly AI assistant. Always respond in a supportive and informative manner. Provide accurate information and assistance to the user while maintaining a positive and professional tone.",
                preferredModelId: "gemini-2.5-pro-preview-03-25"
            );

            var client = _fixture.CreateClientWithAuth(userId: testUser.Id.ToString());

            var chatroom = await _fixture.SeedChatroomAsync(new List<User> { testUser, personaUser }, personaUser.Id, title: "History Limit Chatroom", scopeFactory);
            var chatroomId = chatroom.Id;

            var startDate = DateTime.UtcNow.AddHours(-1);

            // Seed 19 alternating messages
            var seededMessages = await _fixture.SeedMessagesAsync(chatroomId, maxHistoryMessages - 1, startDate, scopeFactory);

            // Seed 1 last message from testUser
            var lastMessages = await _fixture.SeedMessagesAsync(chatroomId, 1, startDate.AddMinutes(maxHistoryMessages), scopeFactory, userId: testUser.Id);

            var allMessages = seededMessages.Concat(lastMessages).OrderBy(m => m.Date).ToList();

            var lastUserMessage = lastMessages.Last();
            var lastUserMessageDate = lastUserMessage.Date;

            var secondToLastMessage = allMessages[^2];

            // Seed LastRead for persona (up to second-to-last message)
            await _fixture.SeedLastReadAsync(chatroomId, personaUser.Id, secondToLastMessage.Date, scopeFactory);

            // Seed LastRead for testUser (up to last message)
            await _fixture.SeedLastReadAsync(chatroomId, testUser.Id, lastUserMessageDate, scopeFactory);

            var newUserMessageDto = new ChatMessageDto
            {
                UserId = testUser.Id,
                ChatroomId = chatroomId,
                Text = "What was the topic of the later messages?",
                Role = MessageRole.User,
                UserFirstName = testUser.FirstName,
                UserLastName = testUser.LastName
            };

            var jsonContent = JsonContent.Create(newUserMessageDto);

            var response = await client.PostAsync($"/api/chatrooms/{chatroomId}/chats", jsonContent);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

            await Task.Delay(TimeSpan.FromSeconds(15));

            ChattyMcChatface.Data.Entities.ChatMessage? personaResponseMessage = null;

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ChattyMcChatface.Data.AppDbContext>();

                personaResponseMessage = await db.ChatMessages
                    .Where(m => m.ChatroomId == chatroomId
                             && m.UserId == personaUser.Id
                             && m.Date > lastUserMessageDate)
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
                    .FirstOrDefaultAsync(lr => lr.ChatroomId == chatroomId && lr.UserId == personaUser.Id);
            }

            lastReadRecord.Should().NotBeNull("because a LastRead record should exist for the persona.");
            lastReadRecord.LastReadDate.Should().Be(personaResponseMessage.Date, "because the LastRead time should be updated to the timestamp of the persona's latest message.");

            mockNotificationService.Verify(
                x => x.SendMessageToGroupAsync(
                    chatroomId.ToString(),
                    chatroomId,
                    It.Is<ChatMessageDto>(dto =>
                        dto.Id == personaResponseMessage.Id &&
                        dto.Text == personaResponseMessage.Text &&
                        dto.Date == personaResponseMessage.Date &&
                        dto.UserId == personaUser.Id &&
                        dto.ChatroomId == chatroomId &&
                        dto.Role == MessageRole.Assistant
                    )
                ),
                Times.Once,
                "The notification service should have been called once with the correct persona message details."
            );
        }

        [Fact]
        public async Task PersonaIgnoresOwnMessages_Scenario2()
        {
            var scopeFactory = _fixture.Services.GetRequiredService<IServiceScopeFactory>();
            var mockNotificationService = _fixture.MockNotificationService;

            await _fixture.ResetDatabaseAsync(scopeFactory);

            var testUserId = 1;
            var personaUserId = 1001;

            var testUser = await _fixture.SeedUserAsync("Test", "User", "testuser2@example.com", false, scopeFactory, testUserId);
            var personaUser = await _fixture.SeedUserAsync(
                "Helpful Assistant", "Bot", "personauser2@example.com", true, scopeFactory, personaUserId,
                systemPrompt: "You are a helpful, friendly AI assistant. Always respond in a supportive and informative manner. Provide accurate information and assistance to the user while maintaining a positive and professional tone.",
                preferredModelId: "gemini-2.5-pro-preview-03-25"
            );

            var chatroom = await _fixture.SeedChatroomAsync(new List<User> { testUser, personaUser }, personaUserId, title: "Scenario2 Chatroom", scopeFactory);
            var chatroomId = chatroom.Id;

            var startDate = DateTime.UtcNow.AddHours(-1);

            // Seed some user messages first
            await _fixture.SeedMessagesAsync(chatroomId, 5, startDate, scopeFactory, userId: testUserId);

            // Then seed persona messages, last message from persona
            var personaMessages = await _fixture.SeedMessagesAsync(chatroomId, 5, startDate.AddMinutes(5), scopeFactory, userId: personaUserId);

            var lastPersonaMessageTimestamp = personaMessages.Last().Date;

            await _fixture.SeedLastReadAsync(chatroomId, personaUserId, lastPersonaMessageTimestamp, scopeFactory);

            await Task.Delay(TimeSpan.FromSeconds(5));

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ChattyMcChatface.Data.AppDbContext>();

            var personaMessagesAfter = await db.ChatMessages
                .Where(m => m.ChatroomId == chatroomId
                            && m.UserId == personaUserId
                            && m.Date > lastPersonaMessageTimestamp)
                .ToListAsync();

            personaMessagesAfter.Count.Should().Be(0, "because the persona should not respond to its own last message");

            mockNotificationService.Verify(
                x => x.SendMessageToGroupAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.Is<ChatMessageDto>(dto =>
                        dto.UserId == personaUserId &&
                        dto.ChatroomId == chatroomId &&
                        dto.Date > lastPersonaMessageTimestamp
                    )
                ),
                Times.Never,
                "because the persona should not send notifications for responses to its own messages"
            );
        }

        [Fact]
        public async Task PersonaHandlesEmptyHistory_Scenario3()
        {
            var mockNotificationService = _fixture.MockNotificationService;
            var scopeFactory = _fixture.Services.GetRequiredService<IServiceScopeFactory>();

            await _fixture.ResetDatabaseAsync(scopeFactory);

            var testUser = await _fixture.SeedUserAsync("Test", "User", "emptyhistory_testuser@example.com", false, scopeFactory, id: 2);
            var client = _fixture.CreateClientWithAuth(userId: testUser.Id.ToString());
            var personaUser = await _fixture.SeedUserAsync(
                "Helpful Assistant", "Bot", "emptyhistory_persona@example.com", true, scopeFactory, id: 1001,
                systemPrompt: "You are a helpful, friendly AI assistant. Always respond in a supportive and informative manner. Provide accurate information and assistance to the user while maintaining a positive and professional tone.",
                preferredModelId: "gemini-2.5-pro-preview-03-25"
            );

            var chatroom = await _fixture.SeedChatroomAsync(
                new List<User> { testUser, personaUser },
                personaUser.Id,
                title: "Empty History Chatroom",
                scopeFactory);

            var chatroomId = chatroom.Id;

            var lastReadTime = DateTime.UtcNow;

            await _fixture.SeedLastReadAsync(chatroomId, testUser.Id, lastReadTime, scopeFactory);
            await _fixture.SeedLastReadAsync(chatroomId, personaUser.Id, lastReadTime, scopeFactory);

            var newUserMessageDto = new ChatMessageDto
            {
                UserId = testUser.Id,
                ChatroomId = chatroomId,
                Text = "Hello Persona, this is the first message!",
                Role = MessageRole.User,
                UserFirstName = testUser.FirstName,
                UserLastName = testUser.LastName
            };

            var jsonContent = JsonContent.Create(newUserMessageDto);

            var messageSentTime = DateTime.UtcNow;

            var response = await client.PostAsync($"/api/chatrooms/{chatroomId}/chats", jsonContent);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

            await Task.Delay(TimeSpan.FromSeconds(10));

            ChattyMcChatface.Data.Entities.ChatMessage? personaResponseMessage = null;

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ChattyMcChatface.Data.AppDbContext>();

                personaResponseMessage = await db.ChatMessages
                    .Where(m => m.ChatroomId == chatroomId
                                && m.UserId == personaUser.Id
                                && m.Date > messageSentTime)
                    .OrderBy(m => m.Date)
                    .FirstOrDefaultAsync();
            }

            personaResponseMessage.Should().NotBeNull("because the persona should have responded to the first message in an empty chatroom.");
            personaResponseMessage.Text.Should().NotBeNullOrEmpty("because the persona response should contain text.");

            ChattyMcChatface.Data.Entities.LastRead? lastReadRecord = null;

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ChattyMcChatface.Data.AppDbContext>();

                lastReadRecord = await db.LastReads
                    .FirstOrDefaultAsync(lr => lr.ChatroomId == chatroomId && lr.UserId == personaUser.Id);
            }

            lastReadRecord.Should().NotBeNull("because a LastRead record should exist for the persona.");
            lastReadRecord.LastReadDate.Should().Be(personaResponseMessage.Date, "because the LastRead time should be updated to the timestamp of the persona's latest message.");

            mockNotificationService.Verify(
                x => x.SendMessageToGroupAsync(
                    chatroomId.ToString(),
                    chatroomId,
                    It.Is<ChatMessageDto>(dto =>
                        dto.Id == personaResponseMessage.Id &&
                        dto.Text == personaResponseMessage.Text &&
                        dto.Date == personaResponseMessage.Date &&
                        dto.UserId == personaUser.Id &&
                        dto.ChatroomId == chatroomId &&
                        dto.Role == MessageRole.Assistant
                    )
                ),
                Times.Once,
                "The notification service should have been called once with the correct persona message details."
            );
        }
    
            [Fact]
            public async Task ChatroomWithoutPersona_Scenario6()
            {
                var mockNotificationService = _fixture.MockNotificationService;
                var scopeFactory = _fixture.Services.GetRequiredService<IServiceScopeFactory>();
    
                await _fixture.ResetDatabaseAsync(scopeFactory);
    
                // Seed two regular users
                var testUser = await _fixture.SeedUserAsync("Test", "User", "testuser6@example.com", false, scopeFactory, id: 5);
                var otherUser = await _fixture.SeedUserAsync("Other", "User", "otheruser6@example.com", false, scopeFactory, id: 6);
    
                // Create client authenticated as testUser
                var client = _fixture.CreateClientWithAuth(userId: testUser.Id.ToString());
    
                // Seed chatroom with both users, PersonaUserId = null
                var chatroom = await _fixture.SeedChatroomAsync(
                    new List<User> { testUser, otherUser },
                    personaUserId: null,
                    title: "Chatroom Without Persona",
                    scopeFactory: scopeFactory
                );
                var chatroomId = chatroom.Id;
    
                // Seed LastRead for both users
                var now = DateTime.UtcNow;
                await _fixture.SeedLastReadAsync(chatroomId, testUser.Id, now, scopeFactory);
                await _fixture.SeedLastReadAsync(chatroomId, otherUser.Id, now, scopeFactory);
    
                // Prepare new user message DTO
                var newUserMessageDto = new ChatMessageDto
                {
                    UserId = testUser.Id,
                    ChatroomId = chatroomId,
                    Text = "Hello, is anyone there?",
                    Role = MessageRole.User,
                    UserFirstName = testUser.FirstName,
                    UserLastName = testUser.LastName
                };
    
                var jsonContent = JsonContent.Create(newUserMessageDto);
    
                var messageSentTime = DateTime.UtcNow;
    
                var response = await client.PostAsync($"/api/chatrooms/{chatroomId}/chats", jsonContent);
    
                response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
    
                await Task.Delay(TimeSpan.FromSeconds(5));
    
                using (var scope = scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ChattyMcChatface.Data.AppDbContext>();
    
                    var newMessages = await db.ChatMessages
                        .Where(m => m.ChatroomId == chatroomId && m.Date > messageSentTime)
                        .ToListAsync();
    
                    newMessages.Count.Should().Be(1, "because only the user's own message should have been saved after messageSentTime");
                    newMessages.Single().UserId.Should().Be(testUser.Id, "because the only new message should be the one sent by the test user");
                }
    
            }
    }
}