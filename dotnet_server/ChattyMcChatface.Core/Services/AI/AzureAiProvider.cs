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
/// Azure OpenAI API provider implementation for AI completions
/// </summary>
public class AzureAiProvider : IAiProvider
{
    private readonly ILogger<AzureAiProvider> _logger;
    private readonly OpenAIClient _client;

    /// <summary>
    /// Initializes a new instance of the AzureAiProvider
    /// </summary>
    /// <param name="configuration">Configuration to retrieve API key and endpoint</param>
    /// <param name="logger">Logger for capturing errors and information</param>
    public AzureAiProvider(IConfiguration configuration, ILogger<AzureAiProvider> logger)
    {
        _logger = logger;
        
        string? apiKey = configuration["AzureOpenAI:ApiKey"];
        string? endpoint = configuration["AzureOpenAI:Endpoint"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Azure OpenAI API key is not configured. Please add 'AzureOpenAI:ApiKey' to configuration.");
        }
        
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new InvalidOperationException("Azure OpenAI endpoint is not configured. Please add 'AzureOpenAI:Endpoint' to configuration.");
        }
        
        // Initialize Azure OpenAI client with endpoint and API key
        _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    /// <inheritdoc />
    public async Task<string> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string modelId)
    {
        try
        {
            _logger.LogInformation("Getting completion from Azure OpenAI deployment {ModelId}", modelId);
            
            // Convert chat history to OpenAI message format
            var messages = new List<ChatRequestMessage>
            {
                // Add system message first
                new ChatRequestSystemMessage(systemPrompt)
            };
            
            // Add conversation history
            foreach (var message in history)
            {
                // For Azure OpenAI, we need to determine the role based on the available information
                // In a real application, you might need more logic to determine the correct role
                messages.Add(new ChatRequestUserMessage(message.Text));
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
            
            // Make API call
            Response<ChatCompletions> response = await _client.GetChatCompletionsAsync(options);
            ChatCompletions completions = response.Value;
            
            if (completions.Choices.Count > 0)
            {
                return completions.Choices[0].Message.Content;
            }
            
            // If no choices were returned, throw an exception
            throw new InvalidOperationException("No completion choices were returned from Azure OpenAI API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion from Azure OpenAI: {Message}", ex.Message);
            throw;
        }
    }
}