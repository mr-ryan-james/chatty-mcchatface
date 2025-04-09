using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services.AI;

/// <summary>
/// Implementation of AI fallback service that tries multiple AI providers in order
/// when the preferred provider fails
/// </summary>
public class AiFallbackService : IAiFallbackService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPersonaConfigService _personaConfigService;
    private readonly ILogger<AiFallbackService> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AiFallbackService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve AI providers</param>
    /// <param name="personaConfigService">Service to access persona configurations</param>
    /// <param name="logger">Logger for the fallback service</param>
    public AiFallbackService(
        IServiceProvider serviceProvider,
        IPersonaConfigService personaConfigService,
        ILogger<AiFallbackService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _personaConfigService = personaConfigService ?? throw new ArgumentNullException(nameof(personaConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<string> GetResponseWithFallbackAsync(PersonaConfig config, List<ChatMessageDto> history)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        
        if (history == null)
            throw new ArgumentNullException(nameof(history));

        // Create the ordered list of models to try (preferred model first, then fallbacks)
        var modelIdsToTry = new List<string> { config.PreferredModelId };
        if (config.FallbackModelIds != null && config.FallbackModelIds.Any())
        {
            modelIdsToTry.AddRange(config.FallbackModelIds);
        }

        _logger.LogInformation("Trying to get AI response with fallback sequence. Persona: {PersonaName}, Preferred Model: {PreferredModel}, Fallbacks: {FallbackModels}",
            config.DisplayName, config.PreferredModelId, string.Join(", ", config.FallbackModelIds ?? new List<string>()));

        var exceptions = new List<Exception>();
        int attempt = 0;

        // Try each model ID in sequence
        foreach (var modelId in modelIdsToTry)
        {
            attempt++;
            try
            {
                _logger.LogDebug("Attempt {Attempt}: Trying model {ModelId}", attempt, modelId);

                // Resolve the appropriate provider for this model ID
                var provider = ResolveProviderForModel(modelId)
                    ?? throw new InvalidOperationException($"Failed to resolve provider for model {modelId}");

                // The null check is no longer needed as we throw above if provider is null

                // Attempt to get a response from this provider
                var response = await provider.GetCompletionAsync(config.SystemPrompt, history, modelId);
                
                _logger.LogInformation("Successfully got response from provider {Provider} using model {ModelId} (attempt {Attempt}/{TotalAttempts})",
                    provider.GetType().Name, modelId, attempt, modelIdsToTry.Count);
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI response from model {ModelId} (attempt {Attempt}/{TotalAttempts}): {ErrorMessage}",
                    modelId, attempt, modelIdsToTry.Count, ex.Message);
                
                exceptions.Add(ex);
            }
        }

        // If we get here, all providers failed
        var errorMessage = $"All AI providers failed to generate a response after {attempt} attempts.";
        _logger.LogError(errorMessage);
        
        throw new AggregateException(errorMessage, exceptions);
    }

    /// <summary>
    /// Resolves the appropriate AI provider implementation for the given model ID
    /// </summary>
    /// <param name="modelId">The model ID to find a provider for</param>
    /// <returns>The appropriate AI provider instance, or null if no suitable provider is found</returns>
    private IAiProvider? ResolveProviderForModel(string modelId)
    {
        // Simple mapping strategy based on model ID naming patterns
        if (string.IsNullOrEmpty(modelId))
        {
            _logger.LogWarning("Empty model ID provided to resolve provider");
            return null;
        }

        modelId = modelId.ToLowerInvariant();

        try
        {
            // Match model ID to the appropriate provider type
            if (modelId.StartsWith("gpt") || modelId.Contains("openai") || modelId.StartsWith("text-"))
            {
                _logger.LogDebug("Using OpenAI provider for model {ModelId}", modelId);
                return _serviceProvider.GetRequiredService<OpenAiProvider>();
            }
            else if (modelId.Contains("azure"))
            {
                _logger.LogDebug("Using Azure AI provider for model {ModelId}", modelId);
                return _serviceProvider.GetRequiredService<AzureAiProvider>();
            }
            else if (modelId.Contains("claude"))
            {
                _logger.LogDebug("Using Claude provider for model {ModelId}", modelId);
                return _serviceProvider.GetRequiredService<ClaudeProvider>();
            }
            else if (modelId.Contains("gemini"))
            {
                _logger.LogDebug("Using Gemini provider for model {ModelId}", modelId);
                return _serviceProvider.GetRequiredService<GeminiProvider>();
            }
            else
            {
                // Get all registered AI providers and see if any can handle this model
                var allProviders = _serviceProvider.GetServices<IAiProvider>();
                
                // For now, default to OpenAI provider as a last resort if it exists
                var openAiProvider = allProviders.FirstOrDefault(p => p is OpenAiProvider);
                if (openAiProvider != null)
                {
                    _logger.LogWarning("Unknown model ID {ModelId}, defaulting to OpenAI provider", modelId);
                    return openAiProvider;
                }
                
                // If even that's not available, return the first provider we can find
                var firstProvider = allProviders.FirstOrDefault();
                if (firstProvider != null)
                {
                    _logger.LogWarning("Unknown model ID {ModelId}, using first available provider: {ProviderType}", 
                        modelId, firstProvider.GetType().Name);
                    return firstProvider;
                }
            }
            
            _logger.LogError("No suitable AI provider found for model {ModelId}", modelId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving provider for model {ModelId}: {ErrorMessage}", modelId, ex.Message);
            return null;
        }
    }
}