using System.Collections.Generic;

namespace ChattyMcChatface.Core.Dtos;

public class UpdateChatroomDto
{
    public required string Title { get; set; }
    public List<int>? AddUserIds { get; set; }
    public List<int>? RemoveUserIds { get; set; }
}