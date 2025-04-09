using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services.AI;

/// <summary>
/// OpenAI API provider implementation for AI completions
/// </summary>
public class OpenAiProvider : IAiProvider
{
    private readonly ILogger<OpenAiProvider> _logger;
    private readonly OpenAIClient _client;

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
    }

    /// <inheritdoc />
    public async Task<string> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string modelId)
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
            
            // Add conversation history
            foreach (var message in history)
            {
                // Determine if message is from user or assistant based on role
                // For simplicity, we assume messages are from users
                // In a real application, you might need more information to determine the role
                messages.Add(new ChatRequestUserMessage(message.Text));
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
            
            if (completions.Choices.Count > 0)
            {
                return completions.Choices[0].Message.Content;
            }
            
            // If no choices were returned, throw an exception
            throw new InvalidOperationException("No completion choices were returned from OpenAI API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion from OpenAI: {Message}", ex.Message);
            throw;
        }
    }
}