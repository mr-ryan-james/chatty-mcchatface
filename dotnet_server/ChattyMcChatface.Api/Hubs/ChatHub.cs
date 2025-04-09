using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Data;
using ChattyMcChatface.Data.Entities;
using System.Security.Claims;
using ChattyMcChatface.Core.Services;

namespace ChattyMcChatface.Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly IPersonaService _personaService;

        public ChatHub(AppDbContext context, IPersonaService personaService)
        {
            _context = context;
            _personaService = personaService;
        }

        public async Task SendMessage(int chatroomId, string message)
        {
            // Get the current user's ID and name from Context.User
            var userId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                throw new HubException("User not found");
            }

            // Find the chatroom entity
            var chatroom = await _context.Chatrooms.FindAsync(chatroomId);
            if (chatroom == null)
            {
                throw new HubException("Chatroom not found");
            }

            // Create and save the message to the database
            var chatMessage = new ChatMessage
            {
                Text = message,
                UserId = userId,
                User = user,
                ChatroomId = chatroomId,
                Chatroom = chatroom
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Create a ChatMessageDto for broadcasting
            var chatMessageDto = new ChatMessageDto
            {
                Id = chatMessage.Id,
                Text = chatMessage.Text,
                Date = chatMessage.Date,
                UserId = chatMessage.UserId,
                UserFirstName = user.FirstName,
                UserLastName = user.LastName,
                ChatroomId = chatMessage.ChatroomId
            };

            // Broadcast the message to all clients connected to the specific chatroom group
            await Clients.Group(chatroomId.ToString()).SendAsync("ReceiveMessage", chatroomId, chatMessageDto);

            // Retrieve the chatroom entity again to ensure PersonaUserId is loaded
            chatroom = await _context.Chatrooms.FindAsync(chatroomId);
            
            // Check if the chatroom has a persona user assigned
            if (chatroom.PersonaUserId.HasValue)
            {
                // Use fire-and-forget pattern to generate a persona response
                _ = Task.Run(() => _personaService.GenerateResponseAsync(chatroomId, chatMessageDto));
            }
        }

        public async Task JoinRoom(int chatroomId)
        {
            // Add the current connection to the SignalR group for the chatroom
            await Groups.AddToGroupAsync(Context.ConnectionId, chatroomId.ToString());
            
            // Optional: Notify others in the group that a user joined
            var userId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            
            if (user != null)
            {
                await Clients.OthersInGroup(chatroomId.ToString()).SendAsync("UserJoined", chatroomId, $"{user.FirstName} {user.LastName}");
            }
        }

        public async Task LeaveRoom(int chatroomId)
        {
            // Remove the current connection from the group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatroomId.ToString());
            
            // Optional: Notify others in the group that a user left
            var userId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            
            if (user != null)
            {
                await Clients.OthersInGroup(chatroomId.ToString()).SendAsync("UserLeft", chatroomId, $"{user.FirstName} {user.LastName}");
            }
        }
    }
}