namespace ChattyMcChatface.Core.Services.AI;

/// <summary>
/// Defines standard identifiers for AI models across different providers
/// </summary>
public static class AiModels
{
    // OpenAI
    public const string OpenAiGpt4oLatest = "chatgpt-4o-latest";
    public const string OpenAiGpt4o2024 = "gpt-4o-2024-11-20";
    public const string OpenAiGpt45Preview = "gpt-4.5-preview-2025-02-27";

    // Azure OpenAI (Using custom identifiers for routing logic)
    public const string AzureGpt4oThrivify = "gpt-4o";
    public const string AzureGpt45PreviewRyan = "gpt-4.5-preview";

    // Claude (Anthropic)
    public const string Claude37Sonnet = "claude-3-7-sonnet-20250219";

    // Gemini (Google)
    public const string Gemini20Flash = "gemini-2.0-flash";
    public const string Gemini25Pro = "gemini-2.5-pro-preview-03-25";

    // Vertex AI (Google)
    public const string Claude37SonnetVertex = "claude-3-7-sonnet@20250219";
    // Add other Vertex models here if needed, e.g., Gemini on Vertex
    // public const string GeminiOnVertex = "gemini-...?@vertex";
}