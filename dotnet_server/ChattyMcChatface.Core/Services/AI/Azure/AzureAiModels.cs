using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services.AI.Azure;

/// <summary>
/// Provides pre-configured functions for specific Azure OpenAI models.
/// This class should be registered as a Singleton and initialized once.
/// </summary>
public class AzureAiModels
{
    // Pre-configured model-specific functions (delegates)
    // Make properties virtual so they can be mocked by Moq
    public virtual Func<string, List<ChatMessageDto>, Task<string?>> Gpt4oThrivify { get; }
    public virtual Func<string, List<ChatMessageDto>, Task<string?>> Gpt45PreviewRyan { get; }

    /// <summary>
    /// Initializes the AzureAiModels class by creating configured delegates using the factory.
    /// Requires IServiceProvider to resolve necessary dependencies (IConfiguration, ILogger).
    /// </summary>
    /// <param name="serviceProvider">The application's service provider.</param>
    public AzureAiModels(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        // Resolve the specific logger for AzureAiProvider
        var logger = serviceProvider.GetRequiredService<ILogger<AzureAiProvider>>(); 

        // Create and store the delegates using the endpoint-specific factory methods
        Gpt4oThrivify = AzureAiProviderFactory.CreateAzureThrivifyCompletionProvider(
            configuration, logger, AiModels.AzureGpt4oThrivify);

        Gpt45PreviewRyan = AzureAiProviderFactory.CreateAzureRyanCompletionProvider(
            configuration, logger, AiModels.AzureGpt45PreviewRyan);
    }
}