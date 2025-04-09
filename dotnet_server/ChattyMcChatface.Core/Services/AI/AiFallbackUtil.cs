using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services.AI;

/// <summary>
/// Utility for handling AI provider fallbacks.
/// </summary>
public static class AiFallbackUtil
{
    /// <summary>
    /// A global fallback priority list of AI models, starting with potentially faster/cheaper models
    /// and proceeding to more advanced models.
    /// </summary>
    public static readonly IReadOnlyList<string> GlobalModelPriority = new List<string>
    {
        AiModels.Gemini20Flash,       // Start with faster models
        AiModels.OpenAiGpt35Turbo,
        AiModels.ClaudeInstant,
        AiModels.OpenAiGpt4oLatest,   // Move to more capable models
        AiModels.Gemini25Pro,
        AiModels.Claude37Sonnet,
        AiModels.Claude37SonnetVertex,
        AiModels.AzureGpt4oThrivify,  // Azure models if available
        AiModels.OpenAiGpt4o2024,     // Advanced models as final fallbacks
        AiModels.OpenAiGpt45Preview,
        AiModels.AzureGpt45PreviewRyan
    };

    /// <summary>
    /// Attempts to get a response using a preferred AI model, falling back to others in a specified priority list if the preferred one fails.
    /// </summary>
    /// <typeparam name="TResult">The expected result type from the handler.</typeparam>
    /// <param name="priorityList">An ordered list of model IDs representing the fallback sequence.</param>
    /// <param name="preferredModelId">The primary model ID to try first.</param>
    /// <param name="handler">An async function that takes a model ID and attempts the AI operation, returning the result or throwing an exception on failure.</param>
    /// <param name="logger">Logger for capturing fallback attempts and errors.</param>
    /// <returns>The result from the first successful handler execution.</returns>
    /// <exception cref="AggregateException">Thrown if all models in the priority list fail.</exception>
    public static async Task<TResult> GetWithFallbackAsync<TResult>(
        IEnumerable<string> priorityList,
        string preferredModelId,
        Func<string, Task<TResult>> handler,
        ILogger logger) where TResult : class // Assuming result can be null for checks
    {
        // Construct the final ordered list, putting the preferred model first
        var orderedModels = new List<string> { preferredModelId };
        orderedModels.AddRange(priorityList.Where(m => m != preferredModelId));

        logger.LogInformation(
            "Starting AI request with fallback. Preferred: {PreferredModel}, Fallback Order: {FallbackOrder}",
            preferredModelId, string.Join(", ", orderedModels.Skip(1)));

        var exceptions = new List<Exception>();
        int attempt = 0;

        foreach (var modelId in orderedModels)
        {
            attempt++;
            try
            {
                logger.LogDebug("Attempt {Attempt}/{TotalAttempts}: Trying model {ModelId}",
                    attempt, orderedModels.Count, modelId);

                // Execute the handler for the current model
                TResult? result = await handler(modelId);

                // Check if the result is considered successful (e.g., not null or empty string if TResult is string)
                bool isSuccess = result != null;
                if (typeof(TResult) == typeof(string))
                {
                    // Consider empty string as failure for string results
                    isSuccess = !string.IsNullOrEmpty(result as string); 
                }

                if (isSuccess)
                {
                    if (attempt > 1)
                    {
                        logger.LogInformation(
                            "Fallback SUCCESS: Attempt {Attempt} using model {ModelId} succeeded after previous failures.",
                            attempt, modelId);
                    }
                    else
                    {
                        logger.LogInformation("Success with preferred model {ModelId} on first attempt.", modelId);
                    }
                    return result!; // Non-null asserted because isSuccess is true
                }
                else
                {
                    // Log failure due to invalid response, but treat as an exception for fallback logic
                    var errorMessage = $"Handler returned null or invalid result for model {modelId}.";
                    logger.LogWarning(errorMessage);
                    exceptions.Add(new InvalidOperationException(errorMessage));
                    // Continue to the next model
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Attempt {Attempt} with model {ModelId} failed: {ErrorMessage}",
                    attempt, modelId, ex.Message);
                exceptions.Add(ex);

                // If this was the last model, log exhaustion
                if (attempt == orderedModels.Count)
                {
                    logger.LogError("AI fallback exhausted. All {TotalAttempts} models failed.", orderedModels.Count);
                }
            }
        }

        // If loop completes, all attempts failed
        var finalErrorMessage = $"All AI models failed after {attempt} attempts.";
        throw new AggregateException(finalErrorMessage, exceptions);
    }
}