using System;
using System.Collections.Generic;

namespace ChattyMcChatface.Core.Dtos;

public record PersonaConfig
{
    public int PersonaUserId { get; init; }
    public required string DisplayName { get; init; }
    public required string SystemPrompt { get; init; }
    public required string PreferredModelId { get; init; }
    public required List<string> FallbackModelIds { get; init; }
}