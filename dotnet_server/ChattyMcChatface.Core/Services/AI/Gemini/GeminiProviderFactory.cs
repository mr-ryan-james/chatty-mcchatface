using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services.AI.Gemini;

/// <summary>
/// Factory for creating configured Gemini (Google) completion functions.
/// </summary>
public static class GeminiProviderFactory
{
    /// <summary>
    /// Creates a function that calls the Gemini provider with a specific model ID and settings.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="httpClientFactory">HTTP client factory for creating HttpClient instances.</param>
    /// <param name="modelId">The specific Gemini model ID to use.</param>
    /// <param name="temperature">The temperature setting for the model (optional).</param>
    /// <returns>An async function delegate for getting completions.</returns>
    public static Func<string, List<ChatMessageDto>, Task<string?>> CreateGeminiCompletionProvider(
        IConfiguration configuration,
        ILogger<GeminiProvider> logger, // Logger specifically for GeminiProvider
        IHttpClientFactory httpClientFactory, // Required for creating HTTP clients
        string modelId,
        double temperature = 0.7) // Default temperature if not specified
    {
        // Note: We create a new provider instance here. If performance becomes an issue,
        // consider injecting the provider instance instead, but that complicates the factory pattern.
        var provider = new GeminiProvider(configuration, logger, httpClientFactory);

        // Return the delegate that captures the provider instance and modelId
        return async (systemPrompt, history) =>
        {
            // TODO: Potentially add temperature or other settings to GetCompletionAsync if needed
            // For now, the provider uses the modelId directly.
            return await provider.GetCompletionAsync(systemPrompt, history, modelId);
        };
    }
}