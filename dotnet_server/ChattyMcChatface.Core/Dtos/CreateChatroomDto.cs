using System.Collections.Generic;

namespace ChattyMcChatface.Core.Dtos;

public class CreateChatroomDto
{
    public required string Title { get; set; }
    public string? PersonaUserId { get; set; }
    public required List<int> UserIds { get; set; }
}