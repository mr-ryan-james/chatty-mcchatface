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

namespace ChattyMcChatface.Core.Services.AI;

/// <summary>
/// OpenAI API provider implementation for AI completions
/// </summary>
public class OpenAiProvider : IAiProvider
{
    private readonly ILogger<OpenAiProvider> _logger;
    private readonly OpenAIClient _client;
    private readonly AsyncRetryPolicy<string?> _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the OpenAiProvider
    /// </summary>
    /// <param name="configuration">Configuration to retrieve API key</param>
    /// <param name="logger">Logger for capturing errors and information</param>
    public OpenAiProvider(IConfiguration configuration, ILogger<OpenAiProvider> logger)
    {
        _logger = logger;
        
        string? apiKey = configuration["OpenAI:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured. Please add 'OpenAI:ApiKey' to configuration.");
        }
        
        _client = new OpenAIClient(apiKey);
        
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
    public async Task<string?> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string modelId)
    {
        // Execute with retry policy
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
            _logger.LogInformation("Getting completion from OpenAI model {ModelId}", modelId);
            
            // Convert chat history to OpenAI message format
            var messages = new List<ChatRequestMessage>
            {
                // Add system message first
                new ChatRequestSystemMessage(systemPrompt)
            };
            
            // Add conversation history based on the message role
            foreach (var message in history)
            {
                // Convert our MessageRole enum to OpenAI message type
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
                DeploymentName = modelId // Use the provided model ID
            };
            
            // Add messages individually to avoid collection initializer error
            foreach (var message in messages)
            {
                options.Messages.Add(message);
            }
            
            // Make API call
            Response<ChatCompletions> response = await _client.GetChatCompletionsAsync(options);
            ChatCompletions completions = response.Value;
            
            // Extract the text content
            if (completions.Choices.Count > 0)
            {
                string extractedText = completions.Choices[0].Message.Content;
                _logger.LogInformation("Successfully extracted text from OpenAI response");
                return extractedText;
            }
            
            _logger.LogWarning("No content found in OpenAI response");
            return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing JSON response from OpenAI: {Message}", ex.Message);
                return null; // Return null if JSON parsing fails
            }
            catch (Exception ex) when (!(ex is RequestFailedException)) // Let RequestFailedException be caught by the retry policy
            {
                _logger.LogError(ex, "Error getting completion from OpenAI: {Message}", ex.Message);
                throw;
            }
        });
    }
}