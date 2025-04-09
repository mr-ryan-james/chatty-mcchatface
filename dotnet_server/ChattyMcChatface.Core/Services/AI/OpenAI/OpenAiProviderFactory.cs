using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services.AI.OpenAI;

/// <summary>
/// Factory for creating configured OpenAI completion functions.
/// </summary>
public static class OpenAiProviderFactory
{
    /// <summary>
    /// Creates a function that calls the OpenAI provider with a specific model ID and settings.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="modelId">The specific OpenAI model ID to use.</param>
    /// <param name="temperature">The temperature setting for the model (optional).</param>
    /// <returns>An async function delegate for getting completions.</returns>
    public static Func<string, List<ChatMessageDto>, Task<string?>> CreateOpenAiCompletionProvider(
        IConfiguration configuration,
        ILogger<OpenAiProvider> logger, // Logger specifically for OpenAiProvider
        string modelId,
        double temperature = 0.7) // Default temperature if not specified
    {
        // Note: We create a new provider instance here. If performance becomes an issue,
        // consider injecting the provider instance instead, but that complicates the factory pattern.
        var provider = new OpenAiProvider(configuration, logger);

        // Return the delegate that captures the provider instance and modelId
        return async (systemPrompt, history) =>
        {
            // TODO: Potentially add temperature or other settings to GetCompletionAsync if needed
            // For now, the provider uses the modelId directly.
            return await provider.GetCompletionAsync(systemPrompt, history, modelId);
        };
    }
}