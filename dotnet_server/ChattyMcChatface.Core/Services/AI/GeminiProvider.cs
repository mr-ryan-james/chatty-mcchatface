using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChattyMcChatface.Core.Services.AI;

/// <summary>
/// Google Gemini API provider implementation for AI completions using REST API
/// </summary>
public class GeminiProvider : IAiProvider
{
    private readonly ILogger<GeminiProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly JsonSerializerOptions _jsonOptions;

    // Base URL for the Gemini API
    private const string BaseApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/";

    /// <summary>
    /// Initializes a new instance of the GeminiProvider using REST API
    /// </summary>
    /// <param name="configuration">Configuration to retrieve API key</param>
    /// <param name="logger">Logger for capturing errors and information</param>
    /// <param name="httpClientFactory">HTTP client factory for creating HttpClient instances</param>
    public GeminiProvider(
        IConfiguration configuration, 
        ILogger<GeminiProvider> logger, 
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClientFactory?.CreateClient("GeminiApi") 
            ?? throw new ArgumentNullException(nameof(httpClientFactory));
        
        _apiKey = configuration["Gemini:ApiKey"] 
            ?? throw new InvalidOperationException("Gemini API key is not configured. Please add 'Gemini:ApiKey' to configuration.");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public async Task<string> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string modelId)
    {
        try
        {
            _logger.LogInformation("Getting completion from Gemini model {ModelId}", modelId);
            
            // Prepare the request URL with the model ID and API key
            var requestUrl = $"{BaseApiUrl}{modelId}:generateContent?key={_apiKey}";
            
            // Create the contents array for the request
            var contents = new List<Content>();
            
            // Add system prompt as first message with system role
            contents.Add(new Content
            {
                Role = "system",
                Parts = new List<Part> { new Part { Text = systemPrompt } }
            });
            
            // Add conversation history
            foreach (var message in history)
            {
                // In a more complete implementation, you would determine the role based on the message
                // For now, we'll assume all messages are from users
                var role = IsUserMessage(message) ? "user" : "model";
                
                contents.Add(new Content
                {
                    Role = role,
                    Parts = new List<Part> { new Part { Text = message.Text } }
                });
            }
            
            // Create the request payload
            var requestPayload = new GenerateContentRequest
            {
                Contents = contents,
                GenerationConfig = new GenerationConfig
                {
                    Temperature = 0.7,
                    MaxOutputTokens = 1024,
                    TopP = 0.95,
                    TopK = 40
                }
            };
            
            // Send the HTTP request
            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestPayload, _jsonOptions);
            
            // Ensure the request was successful
            response.EnsureSuccessStatusCode();
            
            // Parse the response
            var geminiResponse = await response.Content.ReadFromJsonAsync<GenerateContentResponse>(_jsonOptions);
            
            if (geminiResponse == null)
            {
                throw new InvalidOperationException("Failed to parse response from Gemini API");
            }
            
            // Extract the response text from the candidates
            var firstCandidate = geminiResponse.Candidates?.FirstOrDefault();
            var content = firstCandidate?.Content;
            var firstPart = content?.Parts?.FirstOrDefault();
            
            if (firstPart?.Text != null)
            {
                return firstPart.Text;
            }
            
            throw new InvalidOperationException("No candidates were returned from Gemini API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion from Gemini: {Message}", ex.Message);
            throw;
        }
    }
    
    // Helper method to determine if a message is from a user
    private bool IsUserMessage(ChatMessageDto message)
    {
        // In a more complete implementation, you would have a better way to determine the role
        // For now, we'll assume all messages from users (not AI-generated)
        return true;
    }
    
    #region API Models
    
    // Request and response models for Gemini API
    
    private class GenerateContentRequest
    {
        [JsonPropertyName("contents")]
        public List<Content> Contents { get; set; } = new();
        
        [JsonPropertyName("generationConfig")]
        public GenerationConfig? GenerationConfig { get; set; }
        
        [JsonPropertyName("safetySettings")]
        public List<SafetySetting>? SafetySettings { get; set; }
    }
    
    private class GenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }
        
        [JsonPropertyName("maxOutputTokens")]
        public int? MaxOutputTokens { get; set; }
        
        [JsonPropertyName("topP")]
        public double? TopP { get; set; }
        
        [JsonPropertyName("topK")]
        public int? TopK { get; set; }
        
        [JsonPropertyName("stopSequences")]
        public List<string>? StopSequences { get; set; }
        
        [JsonPropertyName("responseMimeType")]
        public string? ResponseMimeType { get; set; }
        
        [JsonPropertyName("responseSchema")]
        public object? ResponseSchema { get; set; }
    }
    
    private class SafetySetting
    {
        [JsonPropertyName("category")]
        public string? Category { get; set; }
        
        [JsonPropertyName("threshold")]
        public string? Threshold { get; set; }
    }
    
    private class Content
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }
        
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new();
    }
    
    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
    
    private class GenerateContentResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
        
        [JsonPropertyName("promptFeedback")]
        public PromptFeedback? PromptFeedback { get; set; }
    }
    
    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
        
        [JsonPropertyName("finishReason")]
        public string? FinishReason { get; set; }
        
        [JsonPropertyName("safetyRatings")]
        public List<SafetyRating>? SafetyRatings { get; set; }
    }
    
    private class SafetyRating
    {
        [JsonPropertyName("category")]
        public string? Category { get; set; }
        
        [JsonPropertyName("probability")]
        public string? Probability { get; set; }
    }
    
    private class PromptFeedback
    {
        [JsonPropertyName("safetyRatings")]
        public List<SafetyRating>? SafetyRatings { get; set; }
    }
    
    #endregion
}