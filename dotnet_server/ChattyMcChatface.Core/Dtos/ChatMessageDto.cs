using System;

namespace ChattyMcChatface.Core.Dtos;

public enum MessageRole
{
    System,
    User,
    Assistant
}

public class ChatMessageDto
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public DateTime Date { get; set; }
    public int UserId { get; set; }
    public required string UserFirstName { get; set; }
    public required string UserLastName { get; set; }
    public int ChatroomId { get; set; }
    public MessageRole Role { get; set; }
}