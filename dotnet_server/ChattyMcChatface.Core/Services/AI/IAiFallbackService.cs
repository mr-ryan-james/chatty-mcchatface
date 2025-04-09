using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;

namespace ChattyMcChatface.Core.Services.AI;

/// <summary>
/// Interface for AI fallback services that provide alternative responses when primary AI provider fails
/// </summary>
public interface IAiFallbackService
{
    /// <summary>
    /// Gets a response using fallback mechanisms when the primary AI provider is unavailable
    /// </summary>
    /// <param name="config">The persona configuration for the AI</param>
    /// <param name="history">The conversation history as a list of messages</param>
    /// <returns>The generated fallback response text</returns>
    Task<string> GetResponseWithFallbackAsync(PersonaConfig config, List<ChatMessageDto> history);
}