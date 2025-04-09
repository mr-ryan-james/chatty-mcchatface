using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.AIPlatform.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ChattyMcChatface.Core.Services.AI;

/// <summary>
/// Google Vertex AI provider implementation using Google.Cloud.AIPlatform.V1 SDK
/// </summary>
public class VertexAiProvider : IAiProvider
{
    private readonly ILogger<VertexAiProvider> _logger;
    private readonly string _projectId;
    private readonly string _location;
    private readonly AsyncRetryPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the VertexAiProvider using Google Cloud SDK
    /// </summary>
    /// <param name="configuration">Configuration to retrieve Project ID and Location</param>
    /// <param name="logger">Logger for capturing errors and information</param>
    public VertexAiProvider(IConfiguration configuration, ILogger<VertexAiProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _projectId = configuration["VertexAI:ProjectId"] 
            ?? throw new InvalidOperationException("Vertex AI Project ID is not configured. Please add 'VertexAI:ProjectId' to configuration.");
        
        _location = configuration["VertexAI:Location"] 
            ?? throw new InvalidOperationException("Vertex AI Location is not configured. Please add 'VertexAI:Location' to configuration.");
        
        // Configure retry policy for transient errors
        _retryPolicy = Policy
            .Handle<Exception>(ex => IsTransientException(ex))
            .WaitAndRetryAsync(
                3, // Retry 3 times
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Retry {RetryCount} after {RetryTime}s delay due to: {Message}",
                        retryCount,
                        timeSpan.TotalSeconds,
                        exception.Message);
                }
            );
    }

    /// <inheritdoc />
    public async Task<string?> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string modelId)
    {
        try
        {
            _logger.LogInformation("Getting completion from Vertex AI model {ModelId}", modelId);
            return await _retryPolicy.ExecuteAsync<string?>(async () =>
            {
                // Create the prediction service client using Application Default Credentials (ADC)
                var predictionServiceClient = await new PredictionServiceClientBuilder
                {
                    Endpoint = $"{_location}-aiplatform.googleapis.com"
                }.BuildAsync();
                
                // Format the model name
                // For Vertex AI, we need to construct the full resource path
                var modelName = $"projects/{_projectId}/locations/{_location}/publishers/google/models/{modelId}";
                
                // Build the content for the request
                var contentList = new List<object>();
                
                // Handle system prompt - Some models use a special system message format
                if (!string.IsNullOrWhiteSpace(systemPrompt))
                {
                    // For Vertex AI, we'll add it as a system message at the beginning
                    contentList.Add(new
                    {
                        role = "system",
                        parts = new[] { new { text = systemPrompt } }
                    });
                }
                
                // Add conversation history
                foreach (var message in history)
                {
                    // Map our MessageRole enum to Vertex AI's expected role values
                    string role = message.Role switch
                    {
                        MessageRole.User => "user",
                        MessageRole.Assistant => "model", // Some models use "model" instead of "assistant"
                        MessageRole.System => "system",
                        _ => throw new ArgumentException($"Unsupported message role: {message.Role}")
                    };
                    
                    contentList.Add(new
                    {
                        role = role,
                        parts = new[] { new { text = message.Text } }
                    });
                }
                
                // Create the request payload structure - varies by model
                var requestContent = new
                {
                    contents = contentList,
                    generation_config = new
                    {
                        temperature = 0.2,
                        max_output_tokens = 1024
                    }
                };
                
                // Serialize to JSON for the API request
                var instances = new List<JsonElement>
                {
                    JsonSerializer.SerializeToElement(requestContent)
                };
                
                // Create the appropriate request payload based on the model type
                var jsonString = JsonSerializer.Serialize(instances);
                // Convert the JSON string to a Google.Protobuf.WellKnownTypes.Value
                var instanceValue = Google.Protobuf.JsonParser.Default.Parse<Google.Protobuf.WellKnownTypes.Value>(jsonString);
                
                // Create the predict request with the model name and instances
                var request = new PredictRequest
                {
                    Endpoint = modelName,
                    Instances = { instanceValue }
                };
                
                // Make the API call to Vertex AI
                var response = await predictionServiceClient.PredictAsync(request);
                
                // Extract and process the response
                if (response?.Predictions != null && response.Predictions.Count > 0)
                {
                    try
                    {
                        // The prediction result structure depends on the model
                        var prediction = response.Predictions[0];
                        
                        // Try to extract the text from the prediction
                        string? responseText = null;
                        
                        // Handle different response formats based on model
                        if (prediction.StructValue?.Fields != null)
                        {
                            // For structured responses (like Gemini)
                            if (prediction.StructValue.Fields.ContainsKey("candidates"))
                            {
                                // Try to extract text from candidates[0].content.parts[0].text
                                var candidatesValue = prediction.StructValue.Fields["candidates"];
                                if (candidatesValue.ListValue?.Values.Count > 0)
                                {
                                    var candidate = candidatesValue.ListValue.Values[0];
                                    Google.Protobuf.WellKnownTypes.Value contentValue = null;
                                    if (candidate.StructValue?.Fields.TryGetValue("content", out contentValue) == true && contentValue != null)
                                    {
                                        Google.Protobuf.WellKnownTypes.Value partsValue = null;
                                        if (contentValue.StructValue?.Fields.TryGetValue("parts", out partsValue) == true && partsValue != null)
                                        {
                                            if (partsValue.ListValue?.Values.Count > 0)
                                            {
                                                var part = partsValue.ListValue.Values[0];
                                                Google.Protobuf.WellKnownTypes.Value partTextValue = null;
                                                if (part.StructValue?.Fields.TryGetValue("text", out partTextValue) == true && partTextValue != null)
                                                {
                                                    responseText = partTextValue.StringValue;
                                                    _logger.LogDebug("Successfully extracted text from candidates[0].content.parts[0].text");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else if (prediction.StructValue.Fields.ContainsKey("content"))
                            {
                                // Try to extract text from content.parts[0].text
                                var contentValue = prediction.StructValue.Fields["content"];
                                Google.Protobuf.WellKnownTypes.Value partsValue = null;
                                if (contentValue.StructValue?.Fields.TryGetValue("parts", out partsValue) == true && partsValue != null)
                                {
                                    if (partsValue.ListValue?.Values.Count > 0)
                                    {
                                        var part = partsValue.ListValue.Values[0];
                                        Google.Protobuf.WellKnownTypes.Value textValue = null;
                                        if (part.StructValue?.Fields.TryGetValue("text", out textValue) == true && textValue != null)
                                        {
                                            responseText = textValue.StringValue;
                                            _logger.LogDebug("Successfully extracted text from content.parts[0].text");
                                        }
                                    }
                                }
                            }
                            else if (prediction.StructValue.Fields.ContainsKey("parts"))
                            {
                                // Try to extract text from parts[0].text
                                var partsValue = prediction.StructValue.Fields["parts"];
                                if (partsValue.ListValue?.Values.Count > 0)
                                {
                                    var part = partsValue.ListValue.Values[0];
                                    Google.Protobuf.WellKnownTypes.Value textValue = null;
                                    if (part.StructValue?.Fields.TryGetValue("text", out textValue) == true && textValue != null)
                                    {
                                        responseText = textValue.StringValue;
                                        _logger.LogDebug("Successfully extracted text from parts[0].text");
                                    }
                                }
                            }
                            else if (prediction.StructValue.Fields.TryGetValue("text", out var textValue))
                            {
                                // Direct text field
                                responseText = textValue.StringValue;
                                _logger.LogDebug("Successfully extracted text from direct text field");
                            }
                        }
                        
                        // If we couldn't extract structured JSON, use the raw string
                        if (responseText == null)
                        {
                            responseText = prediction.ToString();
                        }
                        
                        // Return the extracted text
                        if (!string.IsNullOrEmpty(responseText))
                        {
                            _logger.LogInformation("Successfully extracted text from Vertex AI response");
                            return responseText;
                        }
                        else
                        {
                            _logger.LogWarning("Failed to extract text from Vertex AI response");
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing Vertex AI response: {Message}", ex.Message);
                        return null;
                    }
                }
                
                _logger.LogWarning("No predictions returned from Vertex AI");
                return null;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion from Vertex AI: {Message}", ex.Message);
            return null;
        }
    }
    
    
    /// <summary>
    /// Determines if an exception is transient and should be retried
    /// </summary>
    private bool IsTransientException(Exception ex)
    {
        // Add specific Vertex AI API exception checks as needed
        return ex is TimeoutException
            || ex is System.Net.Http.HttpRequestException
            || (ex is Google.GoogleApiException apiEx && 
                (apiEx.HttpStatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                 apiEx.HttpStatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                 apiEx.HttpStatusCode == System.Net.HttpStatusCode.GatewayTimeout));
    }
}