using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using ChattyMcChatface.Data.Entities;

namespace ChattyMcChatface.Core.Services.AI;

/// <summary>
/// Azure OpenAI API provider implementation for AI completions
/// </summary>
public class AzureAiProvider : IAiProvider
{
    private readonly ILogger<AzureAiProvider> _logger;
    private readonly OpenAIClient _client;
    private readonly AsyncRetryPolicy<string?> _retryPolicy;


    /// <summary>
    /// Initializes a new instance of the AzureAiProvider
    /// </summary>
    /// <param name="apiKey">API key for Azure OpenAI service</param>
    /// <param name="endpoint">Endpoint URL for Azure OpenAI service</param>
    /// <param name="logger">Logger for capturing errors and information</param>
    public AzureAiProvider(string apiKey, string endpoint, ILogger<AzureAiProvider> logger)
    {
        _logger = logger;
        
        // Ensure apiKey and endpoint are validated before use
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("Azure OpenAI API key cannot be null or empty.", nameof(apiKey));
        }
        
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new ArgumentException("Azure OpenAI endpoint cannot be null or empty.", nameof(endpoint));
        }
        
        // Initialize Azure OpenAI client with endpoint and API key
        _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        
        // Configure retry policy for transient errors
        _retryPolicy = Policy<string?>
            .Handle<RequestFailedException>(ex =>
                // Retry on rate limit errors (429) or other transient errors (5xx)
                ex.Status == 429 || (ex.Status >= 500 && ex.Status < 600))
            .WaitAndRetryAsync(
                3, // Retry 3 times
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception.Exception,
                        "Attempt {RetryCount} failed with error {ErrorMessage}. Retrying in {RetryTimeSpan} seconds.",
                        retryCount,
                        exception.Exception?.Message ?? "Unknown error",
                        timeSpan.TotalSeconds);
                });
    }

    /// <inheritdoc />
    public virtual async Task<string?> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string modelId)
    {
        // Execute with retry policy
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
            _logger.LogInformation("Getting completion from Azure OpenAI deployment {ModelId}", modelId);
            
            // Convert chat history to OpenAI message format
            var messages = new List<ChatRequestMessage>
            {
                // Append instruction for JSON mode if necessary (API requires "json" in context)
                new ChatRequestSystemMessage(systemPrompt + "\nEnsure your response is a valid JSON object.")
            };
            
            // Add conversation history based on the message role
            foreach (var message in history)
            {
                // Convert our MessageRole enum to Azure OpenAI message type
                switch (message.Role)
                {
                    case MessageRole.User:
                        messages.Add(new ChatRequestUserMessage(message.Text));
                        break;
                    case MessageRole.Assistant:
                        messages.Add(new ChatRequestAssistantMessage(message.Text));
                        break;
                    case MessageRole.System:
                        messages.Add(new ChatRequestSystemMessage(message.Text));
                        break;
                    default:
                        _logger.LogWarning("Unknown message role: {Role}, treating as user message", message.Role);
                        messages.Add(new ChatRequestUserMessage(message.Text));
                        break;
                }
            }
            
            // Create chat completion options
            var options = new ChatCompletionsOptions
            {
                DeploymentName = modelId // In Azure OpenAI, modelId refers to the deployment name
            };
            
            // Add messages individually to avoid collection initializer error
            foreach (var message in messages)
            {
                options.Messages.Add(message);
            }

            // Set response format to JSON mode
            options.ResponseFormat = ChatCompletionsResponseFormat.JsonObject;
            _logger.LogInformation("Requesting JSON object output for Azure deployment {ModelId}", modelId);
            
            // Make API call
            Response<ChatCompletions> response = await _client.GetChatCompletionsAsync(options);
            ChatCompletions completions = response.Value;
            
            // Extract the text content
            if (completions.Choices.Count > 0)
            {
                string extractedText = completions.Choices[0].Message.Content;
                _logger.LogInformation("Successfully extracted text from Azure OpenAI response");
                return extractedText;
            }
            
            _logger.LogWarning("No content found in Azure OpenAI response");
            return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing JSON response from Azure OpenAI: {Message}", ex.Message);
                return null;
            }
            catch (Exception ex) when (!(ex is RequestFailedException)) // Let RequestFailedException be caught by the retry policy
            {
                _logger.LogError(ex, "Error getting completion from Azure OpenAI: {Message}", ex.Message);
                throw;
            }
        });
    }
}