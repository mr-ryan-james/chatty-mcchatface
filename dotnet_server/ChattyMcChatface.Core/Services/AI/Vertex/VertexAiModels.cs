using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services.AI.Vertex;

/// <summary>
/// Provides pre-configured functions for specific Vertex AI (Google Cloud) models.
/// This class should be registered as a Singleton and initialized once.
/// </summary>
public class VertexAiModels
{
    // Pre-configured model-specific functions (delegates)
    public Func<string, List<ChatMessageDto>, Task<string?>> Claude37SonnetVertex { get; }

    /// <summary>
    /// Initializes the VertexAiModels class by creating configured delegates using the factory.
    /// Requires IServiceProvider to resolve necessary dependencies (IConfiguration, ILogger).
    /// </summary>
    /// <param name="serviceProvider">The application's service provider.</param>
    public VertexAiModels(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        // Resolve the specific logger for VertexAiProvider
        var logger = serviceProvider.GetRequiredService<ILogger<VertexAiProvider>>(); 

        // Create and store the delegates using the factory
        Claude37SonnetVertex = VertexAiProviderFactory.CreateVertexAiCompletionProvider(
            configuration, logger, AiModels.Claude37SonnetVertex, 0.7);
            
        // Add other Vertex AI models here if needed in the future
    }
}