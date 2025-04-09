using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services.AI.Azure;

/// <summary>
/// Factory for creating configured Azure OpenAI completion functions.
/// </summary>
public static class AzureAiProviderFactory
{
    /// <summary>
    /// Creates a function that calls the Azure Thrivify OpenAI provider with a specific model ID.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="modelId">The specific Azure OpenAI model ID to use.</param>
    /// <returns>An async function delegate for getting completions.</returns>
    public static Func<string, List<ChatMessageDto>, Task<string?>> CreateAzureThrivifyCompletionProvider(
        IConfiguration configuration,
        ILogger<AzureAiProvider> logger,
        string modelId)
    {
        string? apiKey = configuration["AzureOpenAI:Thrivify:ApiKey"];
        string? endpoint = configuration["AzureOpenAI:Thrivify:Endpoint"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("AzureOpenAI:Thrivify:ApiKey is missing or empty in the configuration.");
        }
        
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new InvalidOperationException("AzureOpenAI:Thrivify:Endpoint is missing or empty in the configuration.");
        }
        
        // Create a new provider instance with the Thrivify-specific credentials
        var provider = new AzureAiProvider(apiKey, endpoint, logger);
        
        // Return the delegate that calls the provider's GetCompletionAsync method
        return (systemPrompt, history) => provider.GetCompletionAsync(systemPrompt, history, modelId);
    }
    
    /// <summary>
    /// Creates a function that calls the Azure Ryan OpenAI provider with a specific model ID.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="modelId">The specific Azure OpenAI model ID to use.</param>
    /// <returns>An async function delegate for getting completions.</returns>
    public static Func<string, List<ChatMessageDto>, Task<string?>> CreateAzureRyanCompletionProvider(
        IConfiguration configuration,
        ILogger<AzureAiProvider> logger,
        string modelId)
    {
        string? apiKey = configuration["AzureOpenAI:Ryan:ApiKey"];
        string? endpoint = configuration["AzureOpenAI:Ryan:Endpoint"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("AzureOpenAI:Ryan:ApiKey is missing or empty in the configuration.");
        }
        
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new InvalidOperationException("AzureOpenAI:Ryan:Endpoint is missing or empty in the configuration.");
        }
        
        // Create a new provider instance with the Ryan-specific credentials
        var provider = new AzureAiProvider(apiKey, endpoint, logger);
        
        // Return the delegate that calls the provider's GetCompletionAsync method
        return (systemPrompt, history) => provider.GetCompletionAsync(systemPrompt, history, modelId);
    }
}