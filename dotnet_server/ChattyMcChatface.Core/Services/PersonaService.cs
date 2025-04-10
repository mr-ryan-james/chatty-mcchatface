using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Services.AI.Azure;
using ChattyMcChatface.Core.Services.AI.Claude;
using ChattyMcChatface.Core.Services.AI.Gemini;
using ChattyMcChatface.Core.Services.AI.OpenAI;
using ChattyMcChatface.Core.Services.AI.Vertex;
using ChattyMcChatface.Data;
using ChattyMcChatface.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services
{
    public class PersonaService : IPersonaService
    {
        private readonly AppDbContext _dbContext;
        private readonly IPersonaConfigService _personaConfigService;
        private readonly ILogger<PersonaService> _logger;
        private readonly INotificationService _notificationService;
        
        // Model-specific service dependencies
        private readonly OpenAiModels _openAiModels;
        private readonly AzureAiModels _azureAiModels;
        private readonly ClaudeModels _claudeModels;
        private readonly GeminiModels _geminiModels;
        private readonly VertexAiModels _vertexAiModels;

        // Constants
        private const int MaxHistoryMessages = 20;

        public PersonaService(
            AppDbContext dbContext,
            IPersonaConfigService personaConfigService,
            ILogger<PersonaService> logger,
            INotificationService notificationService,
            OpenAiModels openAiModels,
            AzureAiModels azureAiModels,
            ClaudeModels claudeModels,
            GeminiModels geminiModels,
            VertexAiModels vertexAiModels
            )
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _personaConfigService = personaConfigService ?? throw new ArgumentNullException(nameof(personaConfigService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _openAiModels = openAiModels ?? throw new ArgumentNullException(nameof(openAiModels));
            _azureAiModels = azureAiModels ?? throw new ArgumentNullException(nameof(azureAiModels));
            _claudeModels = claudeModels ?? throw new ArgumentNullException(nameof(claudeModels));
            _geminiModels = geminiModels ?? throw new ArgumentNullException(nameof(geminiModels));
            _vertexAiModels = vertexAiModels ?? throw new ArgumentNullException(nameof(vertexAiModels));
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
                    // Get AI response using the fallback utility instead of service
                    // Explicitly specify <string> to satisfy constraints and resolve nullability warnings
                    responseText = await AiFallbackUtil.GetWithFallbackAsync<string>(
                        AiFallbackUtil.GlobalModelPriority, // Use the global priority list
                        config.PreferredModelId,
                        async (modelId) => // Define the handler function (now matches Func<string, Task<string>>)
                        {
                            // Add a switch statement here to call the correct model-specific function
                            // based on the modelId passed by the fallback utility.
                            switch (modelId)
                            {
                                // OpenAI Cases
                                case AiModels.OpenAiGpt4oLatest:
                                    return await _openAiModels.Gpt4oLatest(config.SystemPrompt, historyDtoList) ?? string.Empty;
                                case AiModels.OpenAiGpt4o2024:
                                    return await _openAiModels.Gpt4o2024(config.SystemPrompt, historyDtoList) ?? string.Empty;
                                case AiModels.OpenAiGpt45Preview:
                                    return await _openAiModels.Gpt45Preview(config.SystemPrompt, historyDtoList) ?? string.Empty;

                                // Azure Cases
                                case AiModels.AzureGpt4oThrivify:
                                    return await _azureAiModels.Gpt4oThrivify(config.SystemPrompt, historyDtoList) ?? string.Empty;
                                case AiModels.AzureGpt45PreviewRyan:
                                    return await _azureAiModels.Gpt45PreviewRyan(config.SystemPrompt, historyDtoList) ?? string.Empty;

                                // Claude Cases
                                case AiModels.Claude37Sonnet:
                                    return await _claudeModels.Claude37Sonnet(config.SystemPrompt, historyDtoList) ?? string.Empty;

                                // Gemini Cases
                                case AiModels.Gemini20Flash:
                                    return await _geminiModels.Gemini20Flash(config.SystemPrompt, historyDtoList) ?? string.Empty;
                                case AiModels.Gemini25Pro:
                                    return await _geminiModels.Gemini25Pro(config.SystemPrompt, historyDtoList) ?? string.Empty;

                                // Vertex Cases
                                case AiModels.Claude37SonnetVertex:
                                    return await _vertexAiModels.Claude37SonnetVertex(config.SystemPrompt, historyDtoList) ?? string.Empty;

                                default:
                                    _logger.LogWarning("Handler in AiFallbackUtil encountered unknown modelId: {ModelId}", modelId);
                                    throw new NotSupportedException($"Model ID '{modelId}' is not supported by the handler.");
                            }
                        },
                        _logger // Pass the logger instance
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get AI response for chatroom {ChatroomId}", chatroomId);
                    return;
                }

                // Create a new chat message for the persona's response
                var responseMessage = new ChatMessage
                {
                    Text = responseText ?? string.Empty, // Ensure non-null assignment
                    Date = DateTime.UtcNow,
                    UserId = chatroom.PersonaUserId.Value,
                    ChatroomId = chatroomId,
                    User = null!, // Will be populated by EF Core
                    Chatroom = null! // Will be populated by EF Core
                };

                // Save the response to the database
                await _dbContext.ChatMessages.AddAsync(responseMessage);
                await _dbContext.SaveChangesAsync();
                
                // ---> START ADDED CODE <---
                // Update LastRead status for the persona user
                int personaUserId = chatroom.PersonaUserId.Value;
                var lastRead = await _dbContext.LastReads
                    .FirstOrDefaultAsync(lr => lr.ChatroomId == chatroomId && lr.UserId == personaUserId);

                if (lastRead != null)
                {
                    // Update existing record
                    lastRead.LastReadDate = responseMessage.Date;
                }
                else
                {
                    // Create new record if none exists
                    lastRead = new LastRead
                    {
                        ChatroomId = chatroomId,
                        UserId = personaUserId,
                        LastReadDate = responseMessage.Date
                    };
                    _dbContext.LastReads.Add(lastRead);
                }
                await _dbContext.SaveChangesAsync(); // Save LastRead changes
                // ---> END ADDED CODE <---
                
                // Create a DTO from the saved persona message entity
                // Get persona user details to populate the DTO correctly
                var personaUser = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == chatroom.PersonaUserId.Value);

                if (personaUser != null)
                {
                    // Create message DTO with persona user details
                    var personaMessageDto = new ChatMessageDto
                    {
                        Id = responseMessage.Id!, // Suppress warning: EF Core should populate Id after SaveChangesAsync
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