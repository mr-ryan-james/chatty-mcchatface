using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Data;
using ChattyMcChatface.Data.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace ChattyMcChatface.Tests.Unit.Services
{
    public class PersonaServiceTests
    {
        [Fact]
        public async Task MockQueryable_SupportsLinqOperations_ForDbContextMocking()
        {
            // This test demonstrates that MockQueryable correctly supports LINQ for DbSet mocking
            
            // Arrange - Create test entities with all required properties
            var regularUser = new User 
            { 
                Id = 1, 
                FirstName = "Regular", 
                LastName = "User", 
                Email = "user@example.com", 
                PasswordHash = "hashedpassword123" 
            };
            
            var aiUser = new User 
            { 
                Id = 999, 
                FirstName = "AI", 
                LastName = "Assistant", 
                Email = "ai@example.com", 
                PasswordHash = "aihashedpassword", 
                IsPersona = true 
            };
            
            var users = new List<User> { regularUser, aiUser };
            
            // Create chatroom
            var chatroom = new Chatroom 
            { 
                Id = 1, 
                Title = "Test Chatroom", 
                PersonaUserId = 999,
                PersonaUser = aiUser
            };
            chatroom.Users.Add(regularUser);
            chatroom.Users.Add(aiUser);
            
            var chatrooms = new List<Chatroom> { chatroom };
            
            // ChatMessages need both User and Chatroom
            var message = new ChatMessage 
            { 
                Id = 1, 
                ChatroomId = 1, 
                UserId = 1, 
                Text = "Hello AI!", 
                Date = DateTime.UtcNow.AddMinutes(-5),
                User = regularUser,
                Chatroom = chatroom
            };
            
            var chatMessages = new List<ChatMessage> { message };
            
            // Update references
            chatroom.Chats.Add(message);
            regularUser.Messages.Add(message);
            
            // Build mock DbSets using MockQueryable
            var usersMockDbSet = users.AsQueryable().BuildMockDbSet();
            var chatroomsMockDbSet = chatrooms.AsQueryable().BuildMockDbSet();
            var chatMessagesMockDbSet = chatMessages.AsQueryable().BuildMockDbSet();
            
            // Setup the mock DbContext
            var mockDbContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            mockDbContext.Setup(c => c.Users).Returns(usersMockDbSet.Object);
            mockDbContext.Setup(c => c.Chatrooms).Returns(chatroomsMockDbSet.Object);
            mockDbContext.Setup(c => c.ChatMessages).Returns(chatMessagesMockDbSet.Object);
            
            // Setup AddAsync for ChatMessages without trying to return EntityEntry
            chatMessagesMockDbSet.Setup(d => d.AddAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ChatMessage, CancellationToken>((msg, _) => {
                    // Ensure the new message has the required navigation properties
                    var user = users.FirstOrDefault(u => u.Id == msg.UserId);
                    var room = chatrooms.FirstOrDefault(c => c.Id == msg.ChatroomId);
                    if (user != null) msg.User = user;
                    if (room != null) msg.Chatroom = room;
                    
                    chatMessages.Add(msg);
                    
                    // Update the collections
                    if (user != null) user.Messages.Add(msg);
                    if (room != null) room.Chats.Add(msg);
                })
                .ReturnsAsync((ChatMessage entity, CancellationToken _) => null); // Return null instead of mocking EntityEntry
            
            // Setup SaveChangesAsync
            mockDbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            
            // Act: Test LINQ queries
            var recentMessages = await mockDbContext.Object.ChatMessages
                .Where(m => m.ChatroomId == 1)
                .OrderByDescending(m => m.Date)
                .Take(20)
                .ToListAsync();
            
            var specificChatroom = await mockDbContext.Object.Chatrooms
                .FirstOrDefaultAsync(c => c.Id == 1);
            
            var aiUserFromDb = await mockDbContext.Object.Users
                .FirstOrDefaultAsync(u => u.Id == 999);
            
            // Assert
            recentMessages.Should().NotBeEmpty();
            recentMessages.Should().HaveCount(1);
            specificChatroom.Should().NotBeNull();
            specificChatroom?.PersonaUserId.Should().Be(999);
            aiUserFromDb.Should().NotBeNull();
            aiUserFromDb?.FirstName.Should().Be("AI");
            aiUserFromDb?.LastName.Should().Be("Assistant");
            
            // Test adding a new message
            var newMessage = new ChatMessage
            {
                Id = 2,
                ChatroomId = 1,
                UserId = 999,
                Text = "I am an AI assistant.",
                Date = DateTime.UtcNow,
                User = aiUser,
                Chatroom = chatroom
            };
            
            await chatMessagesMockDbSet.Object.AddAsync(newMessage);
            await mockDbContext.Object.SaveChangesAsync();
            
            // Verify the message was added
            chatMessages.Should().HaveCount(2);
            chatMessages.Should().Contain(newMessage);
            
            // Test we can query for it
            var allMessages = await mockDbContext.Object.ChatMessages
                .Where(m => m.ChatroomId == 1)
                .OrderBy(m => m.Date)
                .ToListAsync();
            
            allMessages.Should().HaveCount(2);
            allMessages[1].Text.Should().Be("I am an AI assistant.");
            allMessages[1].UserId.Should().Be(999);
        }
    }
}