using System;

namespace ChattyMcChatface.Data.Entities
{
    public class LastRead
    {
        public DateTime LastReadDate { get; set; }
        
        // Composite Primary Key parts and Foreign Keys
        public int UserId { get; set; }
        public int ChatroomId { get; set; }
        
        // Navigation properties
        public virtual required User User { get; set; }
        public virtual required Chatroom Chatroom { get; set; }
    }
}