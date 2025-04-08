using System;
using System.Collections.Generic;

namespace ChattyMcChatface.Data.Entities
{
    public class User
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Chatroom> Chatrooms { get; set; } = new List<Chatroom>();
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public virtual ICollection<LastRead> LastReads { get; set; } = new List<LastRead>();
    }
}