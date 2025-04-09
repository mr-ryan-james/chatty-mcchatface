using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Data;
using ChattyMcChatface.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services
{
    public class PersonaService : IPersonaService
    {
        private readonly AppDbContext _dbContext;
        private readonly IAiFallbackService _fallbackService;
        private readonly IPersonaConfigService _personaConfigService;
        private readonly ILogger<PersonaService> _logger;
        private readonly INotificationService _notificationService;

        // Constants
        private const int MaxHistoryMessages = 20;

        public PersonaService(
            AppDbContext dbContext,
            IAiFallbackService fallbackService,
            IPersonaConfigService personaConfigService,
            ILogger<PersonaService> logger,
            INotificationService notificationService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _fallbackService = fallbackService ?? throw new ArgumentNullException(nameof(fallbackService));
            _personaConfigService = personaConfigService ?? throw new ArgumentNullException(nameof(personaConfigService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async Task GenerateResponseAsync(int chatroomId, ChatMessageDto triggeringMessage)
        {
            try
            {
                // Retrieve the chatroom with persona info
                var chatroom = await _dbContext.Chatrooms
                    .FirstOrDefaultAsync(c => c.Id == chatroomId);

                if (chatroom == null)
                {
                    _logger.LogError("Chatroom with ID {ChatroomId} not found", chatroomId);
                    return;
                }

                // Check if persona is assigned to this chatroom
                if (!chatroom.PersonaUserId.HasValue)
                {
                    _logger.LogWarning("Chatroom {ChatroomId} has no persona assigned", chatroomId);
                    return;
                }

                // Get the persona configuration
                var config = _personaConfigService.GetConfig(chatroom.PersonaUserId.Value);
                if (config == null)
                {
                    _logger.LogError("Persona configuration for user ID {PersonaUserId} not found", chatroom.PersonaUserId.Value);
                    return;
                }

                // Fetch recent chat history
                var recentMessages = await _dbContext.ChatMessages
                    .Where(m => m.ChatroomId == chatroomId)
                    .OrderByDescending(m => m.Date)
                    .Take(MaxHistoryMessages)
                    .Include(m => m.User)
                    .ToListAsync();

                // Convert to DTOs for the AI service
                var historyDtoList = recentMessages
                    .OrderBy(m => m.Date)
                    .Select(m => new ChatMessageDto
                    {
                        Id = m.Id,
                        Text = m.Text,
                        Date = m.Date,
                        UserId = m.UserId,
                        UserFirstName = m.User.FirstName,
                        UserLastName = m.User.LastName,
                        ChatroomId = m.ChatroomId,
                        // Set the role based on whether the message is from the persona or a regular user
                        Role = m.UserId == chatroom.PersonaUserId
                            ? MessageRole.Assistant
                            : MessageRole.User
                    })
                    .ToList();

                string responseText;
                try
                {
                    // Get AI response using the fallback service
                    responseText = await _fallbackService.GetResponseWithFallbackAsync(config, historyDtoList);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get AI response for chatroom {ChatroomId}", chatroomId);
                    return;
                }

                // Create a new chat message for the persona's response
                var responseMessage = new ChatMessage
                {
                    Text = responseText,
                    Date = DateTime.UtcNow,
                    UserId = chatroom.PersonaUserId.Value,
                    ChatroomId = chatroomId,
                    User = null!, // Will be populated by EF Core
                    Chatroom = null! // Will be populated by EF Core
                };

                // Save the response to the database
                _dbContext.ChatMessages.Add(responseMessage);
                await _dbContext.SaveChangesAsync();

                // Create a DTO from the saved persona message entity
                // Get persona user details to populate the DTO correctly
                var personaUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == chatroom.PersonaUserId.Value);

                if (personaUser != null)
                {
                    // Create message DTO with persona user details
                    var personaMessageDto = new ChatMessageDto
                    {
                        Id = responseMessage.Id,
                        Text = responseMessage.Text,
                        Date = responseMessage.Date,
                        UserId = responseMessage.UserId,
                        UserFirstName = personaUser.FirstName,
                        UserLastName = personaUser.LastName,
                        ChatroomId = responseMessage.ChatroomId,
                        // This is a message from the AI assistant
                        Role = MessageRole.Assistant
                    };

                    // Send the notification using the notification service
                    await _notificationService.SendMessageToGroupAsync(
                        chatroomId.ToString(),
                        chatroomId,
                        personaMessageDto);
                }
                else
                {
                    _logger.LogError("Persona user with ID {PersonaUserId} not found", chatroom.PersonaUserId.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while generating persona response for chatroom {ChatroomId}", chatroomId);
            }
        }
    }
}