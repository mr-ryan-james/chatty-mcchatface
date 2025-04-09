using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services.AI.OpenAI;

/// <summary>
/// Provides pre-configured functions for specific OpenAI models.
/// This class should be registered as a Singleton and initialized once.
/// </summary>
public class OpenAiModels
{
    // Pre-configured model-specific functions (delegates)
    public Func<string, List<ChatMessageDto>, Task<string?>> Gpt4oLatest { get; }
    public Func<string, List<ChatMessageDto>, Task<string?>> Gpt4o2024 { get; }
    public Func<string, List<ChatMessageDto>, Task<string?>> Gpt45Preview { get; }
    public Func<string, List<ChatMessageDto>, Task<string?>> Gpt35Turbo { get; }

    /// <summary>
    /// Initializes the OpenAiModels class by creating configured delegates using the factory.
    /// Requires IServiceProvider to resolve necessary dependencies (IConfiguration, ILogger).
    /// </summary>
    /// <param name="serviceProvider">The application's service provider.</param>
    public OpenAiModels(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        // Resolve the specific logger for OpenAiProvider
        var logger = serviceProvider.GetRequiredService<ILogger<OpenAiProvider>>(); 

        // Create and store the delegates using the factory
        Gpt4oLatest = OpenAiProviderFactory.CreateOpenAiCompletionProvider(
            configuration, logger, AiModels.OpenAiGpt4oLatest, 0.7);

        Gpt4o2024 = OpenAiProviderFactory.CreateOpenAiCompletionProvider(
            configuration, logger, AiModels.OpenAiGpt4o2024, 0.7);

        Gpt45Preview = OpenAiProviderFactory.CreateOpenAiCompletionProvider(
            configuration, logger, AiModels.OpenAiGpt45Preview, 0.5); // Example different temp

        Gpt35Turbo = OpenAiProviderFactory.CreateOpenAiCompletionProvider(
            configuration, logger, AiModels.OpenAiGpt35Turbo, 0.8); // Example different temp
    }
}