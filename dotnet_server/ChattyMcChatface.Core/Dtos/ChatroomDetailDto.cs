using System;
using System.Collections.Generic;

namespace ChattyMcChatface.Core.Dtos;

public class ChatroomDetailDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public DateTime Date { get; set; }
    public required List<UserDto> Users { get; set; }
    public required List<ChatMessageDto> Messages { get; set; }
}