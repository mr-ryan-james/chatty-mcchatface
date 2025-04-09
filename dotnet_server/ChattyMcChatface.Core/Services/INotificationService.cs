using ChattyMcChatface.Core.Dtos;
using System.Threading.Tasks;

namespace ChattyMcChatface.Core.Services
{
    /// <summary>
    /// Interface for sending real-time notifications to clients
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a message to all clients in a specific group/chatroom
        /// </summary>
        /// <param name="groupId">The group identifier</param>
        /// <param name="chatroomId">The chatroom ID</param>
        /// <param name="message">The message data to send</param>
        Task SendMessageToGroupAsync(string groupId, int chatroomId, ChatMessageDto message);
    }
}