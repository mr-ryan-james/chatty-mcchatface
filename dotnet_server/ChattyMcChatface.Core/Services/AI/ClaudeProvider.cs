using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services.AI;

/// <summary>
/// Anthropic Claude API provider implementation for AI completions
/// </summary>
public class ClaudeProvider : IAiProvider
{
    private readonly ILogger<ClaudeProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    
    // Anthropic API endpoint
    private const string ApiBaseUrl = "https://api.anthropic.com/v1/messages";
    
    /// <summary>
    /// Initializes a new instance of the ClaudeProvider
    /// </summary>
    /// <param name="configuration">Configuration to retrieve API key</param>
    /// <param name="logger">Logger for capturing errors and information</param>
    public ClaudeProvider(IConfiguration configuration, ILogger<ClaudeProvider> logger)
    {
        _logger = logger;
        
        _apiKey = configuration["Anthropic:ApiKey"] 
                 ?? throw new InvalidOperationException("Anthropic API key is not configured. Please add 'Anthropic:ApiKey' to configuration.");
        
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <inheritdoc />
    public async Task<string> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string modelId)
    {
        try
        {
            _logger.LogInformation("Getting completion from Claude model {ModelId}", modelId);
            
            // Convert chat history to Claude message format
            var messages = new List<object>();
            
            // Add conversation history - alternating between user and assistant
            bool isUserMessage = true; // Start with user message
            foreach (var message in history)
            {
                // Claude expects alternating user/assistant messages
                // In a real-world scenario, you'd need more information to determine the correct role
                string role = isUserMessage ? "user" : "assistant";
                
                messages.Add(new
                {
                    role = role,
                    content = message.Text
                });
                
                isUserMessage = !isUserMessage; // Toggle for next message
            }
            
            // Create request body
            var requestBody = new
            {
                model = modelId,
                messages = messages,
                system = systemPrompt,
                max_tokens = 1000 // Configurable parameter if needed
            };
            
            // Serialize request body
            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            // Make API call
            var response = await _httpClient.PostAsync(ApiBaseUrl, content);
            
            // Check if successful
            if (response.IsSuccessStatusCode)
            {
                // Parse response
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseObject = JsonDocument.Parse(responseBody);
                
                // Extract content from Claude's response
                var assistantMessage = responseObject.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
                
                if (!string.IsNullOrEmpty(assistantMessage))
                {
                    return assistantMessage;
                }
                
                throw new InvalidOperationException("No content found in Claude API response");
            }
            
            // Handle API error
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Claude API error: {StatusCode}, {Content}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Claude API error: {response.StatusCode}, {errorContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion from Claude: {Message}", ex.Message);
            throw;
        }
    }
}