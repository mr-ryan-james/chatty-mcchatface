using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ChattyMcChatface.Core.Services.AI;

/// <summary>
/// Google Vertex AI provider implementation using direct HTTP calls to Claude API
/// </summary>
public class VertexAiProvider : IAiProvider
{
    private readonly ILogger<VertexAiProvider> _logger;
    private readonly string _projectId;
    private readonly string _location;
    private readonly string _keyJsonContent;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly string AnthropicPublisher = "anthropic";
    private static readonly string AnthropicApiVersion = "vertex-2023-10-16";
    private static readonly string RawPredictEndpointSuffix = ":rawPredict";
    private static readonly string JsonContentType = "application/json";
    private static readonly string BearerAuthenticationScheme = "Bearer";
    private static readonly string UserRole = "user";
    private static readonly string AssistantRole = "assistant";
    private static readonly string TextContentType = "text";

    /// <summary>
    /// Initializes a new instance of the VertexAiProvider using Google Cloud SDK
    /// </summary>
    /// <param name="configuration">Configuration to retrieve Project ID and Location</param>
    /// <param name="logger">Logger for capturing errors and information</param>
    /// <param name="httpClientFactory">Factory for creating HttpClient instances</param>
    public VertexAiProvider(IConfiguration configuration, ILogger<VertexAiProvider> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        
        _location = configuration["VertexAI:Location"]
            ?? throw new InvalidOperationException("Vertex AI Location is not configured. Please add 'VertexAI:Location' to configuration.");
        string? rawJson = configuration["VertexAI:KeyJsonContent"]; // Allow null
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            throw new InvalidOperationException("Vertex AI Service Account JSON configuration is missing or empty.");
        }
        
        // Log the raw value length for debugging
        _logger.LogDebug("Raw Vertex AI Service Account JSON length: {Length} chars", rawJson.Length);
        _logger.LogDebug("Raw JSON starts with: {Start}", rawJson.Substring(0, Math.Min(50, rawJson.Length)));
        
        // Use the JSON directly without Base64 decoding
        _keyJsonContent = rawJson;
        _logger.LogDebug("Successfully loaded Vertex AI service account JSON.");
        
        // Extract project_id from the KeyJsonContent if it's provided
        try
        {
            if (!string.IsNullOrWhiteSpace(_keyJsonContent))
            {
                var jsonDocument = System.Text.Json.JsonDocument.Parse(_keyJsonContent);
                if (jsonDocument.RootElement.TryGetProperty("project_id", out var projectIdElement))
                {
                    _projectId = projectIdElement.GetString() ?? "";
                }
                // Fallback to configuration value if available
                // No longer using fallback to configuration["VertexAI:ProjectId"]
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Vertex AI Key JSON content to extract project_id.");
            throw new InvalidOperationException("Failed to extract project_id from Vertex AI Key JSON content. The JSON content appears to be invalid.", ex);
        }
        
        // Ensure we have a valid project ID from the service account key JSON
        if (string.IsNullOrWhiteSpace(_projectId))
        {
            throw new InvalidOperationException("Could not determine Vertex AI Project ID. Please ensure it's included in the KeyJsonContent under the 'project_id' field.");
        }
        
        // The 'keyJsonContent' should be the JSON content of the service account key file,
        // typically obtained from Google Cloud IAM. It follows this structure:
        // {
        //   "type": "service_account",
        //   "project_id": "your-project-id",
        //   "private_key_id": "your-private-key-id",
        //   "private_key": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n",
        //   "client_email": "your-service-account-email",
        //   "client_id": "your-client-id",
        //   "auth_uri": "https://accounts.google.com/o/oauth2/auth",
        //   "token_uri": "https://oauth2.googleapis.com/token",
        //   "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
        //   "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/your-service-account-email"
        // }
        
        if (string.IsNullOrWhiteSpace(_keyJsonContent))
        {
            throw new InvalidOperationException("Vertex AI Key JSON Content configuration value is empty.");
        }
        
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
    public virtual async Task<string?> GetCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string modelId)
    {
        try
        {
            _logger.LogInformation("Getting completion from Vertex AI model {ModelId}", modelId);
            return await _retryPolicy.ExecuteAsync<string?>(async () =>
            {
                var credential = CreateCredential();
                var requestPayload = BuildRequestPayload(systemPrompt, history);
                var httpResponse = await MakeApiCallAsync(credential, requestPayload, modelId);
                return await ParseApiResponseAsync(httpResponse);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion from Vertex AI: {Message}", ex.Message);
            return null;
        }
    }
    
    /// <summary>
    /// Creates and returns a GoogleCredential with properly formatted private key
    /// </summary>
    private GoogleCredential CreateCredential()
    {
        _logger.LogDebug("Validating and preparing service account JSON with length: {Length}", _keyJsonContent.Length);
        
        // Validate JSON before passing to GoogleCredential
        try {
            // Make sure we have valid JSON
            var jsonDoc = JsonDocument.Parse(_keyJsonContent);
            // Log some key fields to verify structure (without logging sensitive data)
            if (jsonDoc.RootElement.TryGetProperty("type", out var typeElement)) {
                _logger.LogDebug("Service account JSON type: {Type}", typeElement.GetString());
            }
            if (jsonDoc.RootElement.TryGetProperty("client_email", out var emailElement)) {
                _logger.LogDebug("Service account email: {Email}", emailElement.GetString());
            }
            
            // Specifically check private_key format since that's what's failing
            if (jsonDoc.RootElement.TryGetProperty("private_key", out var privateKeyElement)) {
                string privateKey = privateKeyElement.GetString() ?? "";
                _logger.LogDebug("Private key found, starts with: {PrivateKeyStart}",
                    privateKey.Length > 30 ? privateKey.Substring(0, 30) + "..." : "[empty]");
                // Check if private key has expected format
                if (!privateKey.StartsWith("-----BEGIN PRIVATE KEY-----")) {
                    _logger.LogWarning("Private key does not have expected format, missing BEGIN marker");
                }
            } else {
                _logger.LogWarning("No private_key property found in service account JSON");
            }
        } catch (JsonException jsonEx) {
            _logger.LogError(jsonEx, "Invalid JSON format in service account credentials");
            throw;
        }
        
        try {
            // Parse the JSON to fix the private key format
            var jsonDoc = JsonDocument.Parse(_keyJsonContent);
            string fixedJson = _keyJsonContent;
            
            // Check if we need to fix the private key format
            if (jsonDoc.RootElement.TryGetProperty("private_key", out var privateKeyElement)) {
                string privateKey = privateKeyElement.GetString() ?? "";
                _logger.LogDebug("Original private key length: {Length}", privateKey.Length);
                
                // Make a deep copy of the JSON to avoid modifying the original
                using var memoryStream = new MemoryStream();
                using var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true });
                writer.WriteStartObject();
                
                // First properly fix the private key format
                string fixedPrivateKey = privateKey;
                
                // Step 1: If it contains escaped newlines (\n), replace them with actual newlines
                if (fixedPrivateKey.Contains("\\n")) {
                    _logger.LogDebug("Found escaped newlines in private key, replacing with actual newlines");
                    fixedPrivateKey = fixedPrivateKey.Replace("\\n", "\n");
                }
                
                // Step 2: Ensure it has proper BEGIN/END markers with newlines
                if (!fixedPrivateKey.StartsWith("-----BEGIN PRIVATE KEY-----\n")) {
                    _logger.LogDebug("Fixing BEGIN marker in private key");
                    fixedPrivateKey = fixedPrivateKey.Replace("-----BEGIN PRIVATE KEY-----", "-----BEGIN PRIVATE KEY-----\n");
                }
                
                if (!fixedPrivateKey.EndsWith("\n-----END PRIVATE KEY-----\n")) {
                    _logger.LogDebug("Fixing END marker in private key");
                    fixedPrivateKey = fixedPrivateKey.Replace("-----END PRIVATE KEY-----", "\n-----END PRIVATE KEY-----\n");
                }
                
                // Log the final private key details
                _logger.LogDebug("Fixed private key length: {Length}", fixedPrivateKey.Length);
                _logger.LogDebug("Fixed private key starts with: {Start} and ends with: {End}",
                    fixedPrivateKey.Substring(0, Math.Min(30, fixedPrivateKey.Length)),
                    fixedPrivateKey.Length > 30 ?
                        fixedPrivateKey.Substring(Math.Max(0, fixedPrivateKey.Length - 30)) :
                        "[too short]");
                
                // Write all properties except private_key
                foreach (var prop in jsonDoc.RootElement.EnumerateObject()) {
                    if (prop.Name != "private_key") {
                        prop.WriteTo(writer);
                    }
                }
                
                // Write the fixed private_key
                writer.WritePropertyName("private_key");
                writer.WriteStringValue(fixedPrivateKey);
                
                writer.WriteEndObject();
                writer.Flush();
                
                // Get the fixed JSON
                memoryStream.Position = 0;
                using var reader = new StreamReader(memoryStream);
                fixedJson = reader.ReadToEnd();
                _logger.LogDebug("Created fixed JSON with properly formatted private key");
            }
            
            // Create credential with the fixed JSON
            var credential = GoogleCredential.FromJson(fixedJson)
                .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
            _logger.LogDebug("Successfully created GoogleCredential");
            return credential;
        } catch (Exception ex) {
            _logger.LogError(ex, "Error creating GoogleCredential from JSON: {Message}", ex.Message);
            throw;
        }
    }
    
    /// <summary>
    /// Builds the request payload for the Vertex AI API call
    /// </summary>
    private VertexRequest BuildRequestPayload(string systemPrompt, List<ChatMessageDto> history)
    {
        var messagesList = MapHistoryToVertexMessages(history);
        var requestContent = CreateVertexRequest(systemPrompt, messagesList);

        var requestJson = JsonSerializer.Serialize(requestContent, new JsonSerializerOptions { WriteIndented = true });
        _logger.LogDebug("Request JSON: {Json}", requestJson);

        return requestContent;
    }

    private List<VertexMessage> MapHistoryToVertexMessages(List<ChatMessageDto> history)
    {
        var messagesList = new List<VertexMessage>();

        foreach (var message in history)
        {
            if (message.Role != MessageRole.System)
            {
                string role = message.Role switch
                {
                    MessageRole.User => UserRole,
                    MessageRole.Assistant => AssistantRole,
                    _ => throw new ArgumentException($"Unsupported message role: {message.Role}")
                };

                messagesList.Add(new VertexMessage(
                    role,
                    new List<VertexContentBlock>
                    {
                        new VertexContentBlock(TextContentType, message.Text)
                    }
                ));
            }
        }

        return messagesList;
    }

    private VertexRequest CreateVertexRequest(string systemPrompt, List<VertexMessage> messagesList)
    {
        VertexRequest requestContent;

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            requestContent = new VertexRequest(
                AnthropicVersion: AnthropicApiVersion,
                Messages: messagesList,
                MaxTokens: 1024,
                Temperature: 0.2,
                Stream: false,
                System: systemPrompt
            );
        }
        else
        {
            requestContent = new VertexRequest(
                AnthropicVersion: AnthropicApiVersion,
                Messages: messagesList,
                MaxTokens: 1024,
                Temperature: 0.2,
                Stream: false
            );
        }

        return requestContent;
    }
    
    /// <summary>
    /// Makes the API call to Vertex AI
    /// </summary>
    private async Task<HttpResponseMessage> MakeApiCallAsync(GoogleCredential credential, VertexRequest requestContent, string modelId)
    {
        // Format the request URL for direct API call to Vertex AI's Claude
        // Note: For Claude, we must use 'publishers/anthropic' (not 'publishers/google')
        string requestUrl = $"https://{_location}-aiplatform.googleapis.com/v1/projects/{_projectId}/locations/{_location}/publishers/{AnthropicPublisher}/models/{modelId}{RawPredictEndpointSuffix}";
        _logger.LogDebug("Request URL: {Url}", requestUrl);
        
        // Get an access token for authentication
        string token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        _logger.LogDebug("Got access token of length: {Length}", token?.Length ?? 0);
        
        // Make a direct HTTP request to the Vertex AI Claude endpoint
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(BearerAuthenticationScheme, token);
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(JsonContentType));
        
        // Create the request content
        var requestJson = JsonSerializer.Serialize(requestContent, new JsonSerializerOptions { WriteIndented = true });
        var content = new StringContent(requestJson, System.Text.Encoding.UTF8, JsonContentType);
        
        // Make the API call
        _logger.LogInformation("Sending direct HTTP request to Claude API...");
        return await httpClient.PostAsync(requestUrl, content);
    }
    
    /// <summary>
    /// Parses the API response and extracts the text content
    /// </summary>
    private async Task<string?> ParseApiResponseAsync(HttpResponseMessage httpResponse)
    {
        // Process the response
        _logger.LogInformation("Response status: {StatusCode}", httpResponse.StatusCode);
        
        if (httpResponse.IsSuccessStatusCode)
        {
            var responseBody = await httpResponse.Content.ReadAsStringAsync();
            _logger.LogDebug("Raw response: {Response}", responseBody);
            
            // Parse the response JSON
            try
            {
                var vertexResponse = JsonSerializer.Deserialize<VertexResponse>(responseBody);
                
                if (vertexResponse?.Content?.Count > 0 && vertexResponse.Content[0].Type == "text")
                {
                    string text = vertexResponse.Content[0].Text ?? "";
                    _logger.LogInformation("Successfully extracted text from Vertex response DTO");
                    return text;
                }
                
                _logger.LogWarning("Could not find expected content structure in response");
                return null;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error parsing Claude API response: {Message}", jsonEx.Message);
                return null;
            }
        }
        else
        {
            // Log the error response
            string errorContent = await httpResponse.Content.ReadAsStringAsync();
            _logger.LogError("Claude API error: {StatusCode}, Response: {Response}",
                httpResponse.StatusCode, errorContent);
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
    
    #region DTOs
    
    // Request DTOs
    internal record VertexRequest(
        [property: JsonPropertyName("anthropic_version")] string AnthropicVersion,
        [property: JsonPropertyName("messages")] List<VertexMessage> Messages,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("system"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? System = null // Optional system prompt
    );

    internal record VertexMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] List<VertexContentBlock> Content
    );

    internal record VertexContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string Text
    );

    // Response DTOs
    internal record VertexResponse(
        [property: JsonPropertyName("content")] List<VertexContentBlock> Content
        // Add other fields like 'usage' if needed later
    );
    
    #endregion
}