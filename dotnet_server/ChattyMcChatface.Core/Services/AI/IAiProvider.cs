using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;

namespace ChattyMcChatface.Core.Services.AI;

/// <summary>
/// Interface for AI provider services that can generate text completions
/// </summary>
public interface IAiProvider
{
    /// <summary>
    /// Gets a completion from an AI model based on the system prompt and conversation history
    /// </summary>
    /// <param name="systemPrompt">The system instructions for the AI model</param>
    /// <param name="history">The conversation history as a list of messages</param>
    /// <param name="modelId">The identifier for the specific AI model to use</param>
    /// <returns>The generated completion text</returns>
    Task<string> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string modelId);
}