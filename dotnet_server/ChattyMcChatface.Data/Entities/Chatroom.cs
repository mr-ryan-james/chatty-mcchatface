using System;
using System.Collections.Generic;

namespace ChattyMcChatface.Data.Entities
{
    public class Chatroom
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public int? PersonaUserId { get; set; }
        
        // Navigation properties
        public virtual User? PersonaUser { get; set; }
        public virtual ICollection<ChatMessage> Chats { get; set; } = new List<ChatMessage>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<LastRead> LastReads { get; set; } = new List<LastRead>();
    }
}