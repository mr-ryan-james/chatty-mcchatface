using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services.AI.Claude;

/// <summary>
/// Provides pre-configured functions for specific Claude (Anthropic) models.
/// This class should be registered as a Singleton and initialized once.
/// </summary>
public class ClaudeModels
{
    // Pre-configured model-specific functions (delegates)
    public Func<string, List<ChatMessageDto>, Task<string?>> Claude37Sonnet { get; }
    public Func<string, List<ChatMessageDto>, Task<string?>> ClaudeInstant { get; }

    /// <summary>
    /// Initializes the ClaudeModels class by creating configured delegates using the factory.
    /// Requires IServiceProvider to resolve necessary dependencies (IConfiguration, ILogger).
    /// </summary>
    /// <param name="serviceProvider">The application's service provider.</param>
    public ClaudeModels(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        // Resolve the specific logger for ClaudeProvider
        var logger = serviceProvider.GetRequiredService<ILogger<ClaudeProvider>>(); 

        // Create and store the delegates using the factory
        Claude37Sonnet = ClaudeProviderFactory.CreateClaudeCompletionProvider(
            configuration, logger, AiModels.Claude37Sonnet, 0.7);

        ClaudeInstant = ClaudeProviderFactory.CreateClaudeCompletionProvider(
            configuration, logger, AiModels.ClaudeInstant, 0.8); // Example different temp
    }
}