using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services.AI.Gemini;

/// <summary>
/// Provides pre-configured functions for specific Gemini (Google) models.
/// This class should be registered as a Singleton and initialized once.
/// </summary>
public class GeminiModels
{
    // Pre-configured model-specific functions (delegates)
    public Func<string, List<ChatMessageDto>, Task<string?>> Gemini20Flash { get; }
    public Func<string, List<ChatMessageDto>, Task<string?>> Gemini25Pro { get; }

    /// <summary>
    /// Initializes the GeminiModels class by creating configured delegates using the factory.
    /// Requires IServiceProvider to resolve necessary dependencies (IConfiguration, ILogger, IHttpClientFactory).
    /// </summary>
    /// <param name="serviceProvider">The application's service provider.</param>
    public GeminiModels(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        // Resolve the specific logger for GeminiProvider
        var logger = serviceProvider.GetRequiredService<ILogger<GeminiProvider>>();
        // Resolve the HTTP client factory (required by GeminiProvider)
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Create and store the delegates using the factory
        Gemini20Flash = GeminiProviderFactory.CreateGeminiCompletionProvider(
            configuration, logger, httpClientFactory, AiModels.Gemini20Flash, 0.7);

        Gemini25Pro = GeminiProviderFactory.CreateGeminiCompletionProvider(
            configuration, logger, httpClientFactory, AiModels.Gemini25Pro, 0.5); // Example different temp
    }
}