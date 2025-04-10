using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace ChattyMcChatface.Core.Services.AI.Vertex;

/// <summary>
/// Factory for creating configured Vertex AI (Google Cloud) completion functions.
/// </summary>
public static class VertexAiProviderFactory
{
    /// <summary>
    /// Creates a function that calls the Vertex AI provider with a specific model ID and settings.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="modelId">The specific Vertex AI model ID to use.</param>
    /// <param name="temperature">The temperature setting for the model (optional).</param>
    /// <returns>An async function delegate for getting completions.</returns>
    public static Func<string, List<ChatMessageDto>, Task<string?>> CreateVertexAiCompletionProvider(
        IConfiguration configuration,
        ILogger<VertexAiProvider> logger, // Logger specifically for VertexAiProvider
        IHttpClientFactory httpClientFactory,
        string modelId)
    {
        // Note: We create a new provider instance here. If performance becomes an issue,
        // consider injecting the provider instance instead, but that complicates the factory pattern.
        var provider = new VertexAiProvider(configuration, logger, httpClientFactory);

        // Return the delegate that captures the provider instance and modelId
        return async (systemPrompt, history) =>
        {
            return await provider.GetCompletionAsync(systemPrompt, history, modelId);
        };
    }
}