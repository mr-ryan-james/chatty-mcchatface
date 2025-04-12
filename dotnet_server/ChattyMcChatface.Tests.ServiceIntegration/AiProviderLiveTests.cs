using Xunit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions; // Use NullLogger
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Core.Dtos;

namespace ChattyMcChatface.Tests.ServiceIntegration
{
    public class AiProviderLiveTests
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OpenAiProvider _openAiProvider;
        private readonly GeminiProvider _geminiProvider;
        private readonly ClaudeProvider _claudeProvider;
        private readonly VertexAiProvider _vertexAiProvider;
        private readonly AzureAiProvider _azureThrivifyProvider;
        private readonly AzureAiProvider? _azureRyanProvider; // Make nullable for safety

        public AiProviderLiveTests()
        {
            // Build configuration that includes user secrets
            // Assumes UserSecretsId is set in the .csproj or environment
            _configuration = new ConfigurationBuilder()
                .AddUserSecrets<AiProviderLiveTests>() // Uses the UserSecretsId from this project's .csproj
                .Build();

            // Create a real HttpClientFactory
            var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
            _httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            // Instantiate real providers using configuration and NullLogger
            _openAiProvider = new OpenAiProvider(_configuration, NullLogger<OpenAiProvider>.Instance);
            _geminiProvider = new GeminiProvider(_configuration, NullLogger<GeminiProvider>.Instance, _httpClientFactory);
            _claudeProvider = new ClaudeProvider(_configuration, NullLogger<ClaudeProvider>.Instance);
            _vertexAiProvider = new VertexAiProvider(_configuration, NullLogger<VertexAiProvider>.Instance, _httpClientFactory);

            // Instantiate Azure provider(s) using specific config sections
            var azureThrivifyApiKey = _configuration["AzureOpenAI:Thrivify:ApiKey"];
            var azureThrivifyEndpoint = _configuration["AzureOpenAI:Thrivify:Endpoint"];
            if (!string.IsNullOrEmpty(azureThrivifyApiKey) && !string.IsNullOrEmpty(azureThrivifyEndpoint))
            {
                _azureThrivifyProvider = new AzureAiProvider(azureThrivifyApiKey, azureThrivifyEndpoint, NullLogger<AzureAiProvider>.Instance);
            }
            else
            {
                 // Handle missing config for Thrivify - maybe throw or log, or skip tests?
                 // For now, let it be null and handle in GetProvidersAndModels
                 _azureThrivifyProvider = null!; // Use null-forgiving operator, check in MemberData
            }

            var azureRyanApiKey = _configuration["AzureOpenAI:Ryan:ApiKey"];
            var azureRyanEndpoint = _configuration["AzureOpenAI:Ryan:Endpoint"];
            if (!string.IsNullOrEmpty(azureRyanApiKey) && !string.IsNullOrEmpty(azureRyanEndpoint))
            {
                _azureRyanProvider = new AzureAiProvider(azureRyanApiKey, azureRyanEndpoint, NullLogger<AzureAiProvider>.Instance);
            }
            else
            {
                _azureRyanProvider = null; // Explicitly null if config missing
                // Consider logging a warning here if Ryan config is expected
            }
        }
public static IEnumerable<object[]> GetProvidersAndModels()
{
    var tests = new AiProviderLiveTests(); // Create instance to access configured providers

    // OpenAI Models
    yield return new object[] { tests._openAiProvider, AiModels.OpenAiGpt4oLatest, "OpenAI GPT-4o Latest" }; // No native structured output support (uses JSON mode)
    yield return new object[] { tests._openAiProvider, AiModels.OpenAiGpt4o2024, "OpenAI GPT-4o 2024" }; // Native structured output support (but provider uses JSON mode)
    yield return new object[] { tests._openAiProvider, AiModels.OpenAiGpt45Preview, "OpenAI GPT-4.5 Preview" }; // Native structured output support (but provider uses JSON mode)

    // Gemini Models
    yield return new object[] { tests._geminiProvider, AiModels.Gemini20Flash, "Gemini 2.0 Flash" }; // Supports structured output
    yield return new object[] { tests._geminiProvider, AiModels.Gemini25Pro, "Gemini 2.5 Pro" }; // Supports structured output

    // Claude Models (Anthropic & Vertex)
    yield return new object[] { tests._claudeProvider, AiModels.Claude37Sonnet, "Claude 3.7 Sonnet (Anthropic)" }; // Needs prompt guidance
    yield return new object[] { tests._vertexAiProvider, AiModels.Claude37SonnetVertex, "Claude 3.7 Sonnet (Vertex)" }; // Needs prompt guidance

    // Azure Models
    if (tests._azureThrivifyProvider != null)
    {
        yield return new object[] { tests._azureThrivifyProvider, AiModels.AzureGpt4oThrivify, "Azure OpenAI (Thrivify GPT-4o)" }; // Use deployment name from AiModels
    }
    if (tests._azureRyanProvider != null)
    {
        yield return new object[] { tests._azureRyanProvider, AiModels.AzureGpt45PreviewRyan, "Azure OpenAI (Ryan GPT-4.5 Preview)" }; // Use deployment name from AiModels
    }
}

[Theory]
[MemberData(nameof(GetProvidersAndModels))]
[Trait("Category", "LiveApi")]
public async Task Provider_ReturnsValidStructuredJson_WhenPrompted(IAiProvider provider, string modelId, string providerDescription)
{
    await RunStructuredJsonTestAsync(provider, modelId, providerDescription);
}

// Helper method containing the core test logic
        // Helper method containing the core test logic
        private static async Task RunStructuredJsonTestAsync(IAiProvider provider, string modelId, string providerDescription)
        {
            // Arrange
            string systemPrompt = """
            You are an AI assistant. Your ONLY task is to respond with a valid JSON object matching the structure below.
            Do NOT include any text, explanation, or markdown formatting before or after the JSON object.
            Output ONLY the JSON object itself.

            JSON Structure:
            {{
              "ai_persona": "string",
              "response_type": "string",
              "message": "string"
            }}

            Example:
            {{
              "ai_persona": "JsonValidator",
              "response_type": "ValidationSuccess",
              "message": "The JSON structure is correct."
            }}

            Now, provide a response for the user's input "hello", adhering strictly to this JSON format.
            """;

            var chatHistory = new List<ChatMessageDto>
            {
                 new ChatMessageDto { Role = MessageRole.User, Text = "hello", UserId = 0, UserFirstName = "Test", UserLastName = "User" }
            };

            // Act
            string responseString = string.Empty;
            Func<Task> act = async () => responseString = await provider.GetCompletionAsync(systemPrompt, chatHistory, modelId);

            // Assert - Check if the API call itself throws (e.g., auth error)
            await act.Should().NotThrowAsync($"because the API call to {providerDescription} ({modelId}) should succeed");

            // Assert - Check the response string format
            responseString.Should().NotBeNullOrWhiteSpace($"because {providerDescription} ({modelId}) should return a response");

            // Assert - Check JSON Deserialization
            AiStructuredResponseDto? result = null;
            try
            {
                result = JsonSerializer.Deserialize<AiStructuredResponseDto>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                Assert.Fail($"Deserialization failed for {providerDescription} ({modelId}). Error: {ex.Message}. Raw Response:\n---\n{responseString}\n---");
            }

            // Assert - Check Deserialized Object and Properties
            result.Should().NotBeNull($"because the JSON from {providerDescription} ({modelId}) should deserialize successfully");
            result!.AiPersona.Should().NotBeNullOrWhiteSpace($"because 'ai_persona' in the JSON from {providerDescription} ({modelId}) should not be empty. Raw Response:\n---\n{responseString}\n---");
            result!.ResponseType.Should().NotBeNullOrWhiteSpace($"because 'response_type' in the JSON from {providerDescription} ({modelId}) should not be empty. Raw Response:\n---\n{responseString}\n---");
            result!.Message.Should().NotBeNullOrWhiteSpace($"because 'message' in the JSON from {providerDescription} ({modelId}) should not be empty. Raw Response:\n---\n{responseString}\n---");
        }
    }
}