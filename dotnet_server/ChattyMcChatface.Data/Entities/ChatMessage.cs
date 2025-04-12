using System;

namespace ChattyMcChatface.Data.Entities
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public required string Text { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public MessageRole Role { get; set; } = MessageRole.User; // Default to User
        
        // Foreign keys
        public int UserId { get; set; }
        public int ChatroomId { get; set; }
        
        // Navigation properties
        public virtual required User User { get; set; }
        public virtual required Chatroom Chatroom { get; set; }
    }
}