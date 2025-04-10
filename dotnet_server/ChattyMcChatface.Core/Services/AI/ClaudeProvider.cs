using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ChattyMcChatface.Core.Services.AI;

/// <summary>
/// Anthropic Claude API provider implementation for AI completions using the REST API
/// </summary>
public class ClaudeProvider : IAiProvider
{
    private readonly ILogger<ClaudeProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    
    // Anthropic API endpoint and version
    private const string ApiBaseUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";
    
    /// <summary>
    /// Initializes a new instance of the ClaudeProvider
    /// </summary>
    /// <param name="configuration">Configuration to retrieve API key</param>
    /// <param name="logger">Logger for capturing errors and information</param>
    public ClaudeProvider(IConfiguration configuration, ILogger<ClaudeProvider> logger)
    {
        _logger = logger;
        
        _apiKey = configuration["Anthropic:ApiKey"]
                 ?? throw new InvalidOperationException("Claude API key is not configured. Please add 'Anthropic:ApiKey' to configuration.");
        
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        // Configure retry policy for transient errors
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r =>
                (int)r.StatusCode >= 500 || // Server errors
                r.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                r.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // Rate limiting
            .WaitAndRetryAsync(
                3, // Retry 3 times
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                onRetry: (outcome, timeSpan, retryAttempt, context) =>
                {
                    _logger.LogWarning(
                        "Retrying Claude API request after {RetryAttempt} attempts due to {StatusCode}. Waiting {TimeSpan} before next retry.",
                        retryAttempt,
                        outcome.Result?.StatusCode.ToString() ?? "unknown error",
                        timeSpan);
                });
    }

    /// <inheritdoc />
    public virtual async Task<string?> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string modelId)
    {
        try
        {
            _logger.LogInformation("Getting completion from Claude model {ModelId}", modelId);
            
            // Convert chat history to Claude message format
            var messages = new List<Message>();
            
            // Add conversation history using the message role
            foreach (var message in history)
            {
                // Map our MessageRole enum to Claude's expected role strings
                string role = message.Role switch
                {
                    MessageRole.User => "user",
                    MessageRole.Assistant => "assistant",
                    // System messages should be handled separately as Claude doesn't support system in the messages array
                    MessageRole.System => throw new ArgumentException("System messages should be provided separately, not in the conversation history"),
                    _ => throw new ArgumentException($"Unsupported message role: {message.Role}")
                };
                
                // Create a message with the appropriate role and content
                messages.Add(new Message
                {
                    Role = role,
                    Content = message.Text
                });
            }
            
            // Add the current system prompt as a user message
            messages.Add(new Message
            {
                Role = "user",
                Content = systemPrompt
            });

            // Create request body
            var requestBody = new ClaudeRequest
            {
                Model = modelId,
                Messages = messages,
                System = null, // System prompt is now sent as a user message
                MaxTokens = 1000 // Configurable parameter if needed
            };
            
            // Serialize request body
            var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            // Make API call with retry policy
            var response = await _retryPolicy.ExecuteAsync(() => _httpClient.PostAsync(ApiBaseUrl, content));
            
            // Check if successful
            if (response.IsSuccessStatusCode)
            {
                // Parse the response
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Claude API response: {Response}", responseBody);
                
                // Extract the text content from Claude's response
                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    if (doc.RootElement.TryGetProperty("content", out var contentElement))
                    {
                        if (contentElement.ValueKind == JsonValueKind.Array && contentElement.GetArrayLength() > 0)
                        {
                            var firstContent = contentElement[0];
                            if (firstContent.TryGetProperty("text", out var textElement))
                            {
                                string extractedText = textElement.GetString() ?? string.Empty;
                                _logger.LogInformation("Successfully extracted text from Claude response");
                                return extractedText;
                            }
                        }
                    }
                    
                    _logger.LogWarning("Could not extract text content from Claude response");
                    return null;
                }
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
    
    #region Claude API Classes
    
    private class ClaudeRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";
        
        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; } = new();
        
        [JsonPropertyName("system")]
        public string? System { get; set; }
        
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1000;
    }
    
    private class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";
        
        [JsonPropertyName("content")]
        public string? Content { get; set; } = null;
    }
    
    
    #endregion
}