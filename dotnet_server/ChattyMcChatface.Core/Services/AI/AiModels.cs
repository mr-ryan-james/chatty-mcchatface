namespace ChattyMcChatface.Core.Services.AI;

/// <summary>
/// Defines standard identifiers for AI models across different providers
/// </summary>
public static class AiModels
{
    // OpenAI

    //"chatgpt-4o-latest" does not support structured output
    public const string OpenAiGpt4oLatest = "chatgpt-4o-latest";
    // "gpt-4o-2024-11-20" supports structured output
    public const string OpenAiGpt4o2024 = "gpt-4o-2024-11-20";
    // "gpt-4.5-preview-2025-02-27" supports structured output
    public const string OpenAiGpt45Preview = "gpt-4.5-preview-2025-02-27";

    // Azure OpenAI (Using custom identifiers for routing logic)
    // "gpt-4o" from azure supports structured output
    public const string AzureGpt4oThrivify = "gpt-4o";
    // "gpt-4.5-preview" from azure supports structured output
    public const string AzureGpt45PreviewRyan = "gpt-4.5-preview";

    // Claude (Anthropic)
    // does not support structured output, must be guided by system prompt, etc
    public const string Claude37Sonnet = "claude-3-7-sonnet-20250219";

    // Gemini (Google)
    // does support structured output
    public const string Gemini20Flash = "gemini-2.0-flash";
    // does support structured output
    public const string Gemini25Pro = "gemini-2.5-pro-preview-03-25";

    // Vertex AI (Google)
    // does not support structured output, must be guided by system prompt, etc
    public const string Claude37SonnetVertex = "claude-3-7-sonnet@20250219";
    // Add other Vertex models here if needed, e.g., Gemini on Vertex
    // public const string GeminiOnVertex = "gemini-...?@vertex";
}