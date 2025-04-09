using System;
using System.Threading.Tasks;
using ChattyMcChatface.Api.Hubs;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services;
using Microsoft.AspNetCore.SignalR;

namespace ChattyMcChatface.Api.Services
{
    /// <summary>
    /// Implementation of INotificationService that uses SignalR to send notifications to clients
    /// </summary>
    public class SignalRNotificationService : INotificationService
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public SignalRNotificationService(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        /// <summary>
        /// Sends a chat message to all clients in a specific group/chatroom
        /// </summary>
        public async Task SendMessageToGroupAsync(string groupId, int chatroomId, ChatMessageDto message)
        {
            if (string.IsNullOrEmpty(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));
            
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // Send the message to all clients in the specified group
            await _hubContext.Clients.Group(groupId)
                .SendAsync("ReceiveMessage", chatroomId, message);
        }
    }
}