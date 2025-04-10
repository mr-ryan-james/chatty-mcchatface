# ChattyMcChatface - AI Provider Development Guide

## 1. Introduction

This document serves as a reference guide for developers working with the AI provider implementation
within the ChattyMcChatface .NET project. It covers the system architecture, testing strategies,
configuration management, and key considerations for maintaining and extending the AI provider
functionality.

## 2. System Overview

The core functionality involves generating text responses using various AI models. This process is
orchestrated by `PersonaService`, which utilizes a fallback utility (`AiFallbackUtil`) and
model-specific delegates provided by singleton model classes.

### 2.1. Core Components

-   **`AiModels.cs`:** Defines constants for all supported AI model identifiers (e.g.,
    `OpenAiModels.Gpt4oLatest`, `AiModels.Claude37SonnetVertex`). Note the special `@` versioning
    format for Vertex AI models.
-   **Provider Factories (e.g., `OpenAiProviderFactory.cs`):** Static classes responsible for
    creating `Func<string, List<ChatMessageDto>, Task<string?>>` delegates. These delegates are
    pre-configured for a specific provider and model ID, encapsulating the instantiation and
    configuration of the underlying provider (e.g., `OpenAiProvider`, `VertexAiProvider`).
-   **Model Classes (e.g., `OpenAiModels.cs`, `VertexAiModels.cs`):** Singleton classes injected via
    Dependency Injection (DI). During initialization, they use the corresponding Provider Factory to
    create and store the pre-configured `Func` delegates for each specific model variant supported
    by that provider (e.g., `OpenAiModels.Gpt4oLatest`, `VertexAiModels.Claude37Sonnet`).
-   **`AiFallbackUtil.cs`:** Contains the static `GetWithFallbackAsync` method. This utility manages
    the process of trying different AI models based on a priority list until a successful response
    is generated or all options are exhausted.
-   **`PersonaService.cs`:** The central service orchestrating AI responses. It injects the
    singleton Model Classes (e.g., `OpenAiModels`, `VertexAiModels`). Its `GenerateResponseAsync`
    method uses `AiFallbackUtil.GetWithFallbackAsync`, providing a handler function. This handler
    uses a `switch` statement based on the model ID to invoke the correct pre-configured delegate
    from the injected Model Classes (e.g., `_vertexAiModels.Claude37Sonnet(...)`).
-   **Base Providers (e.g., `OpenAiProvider.cs`, `VertexAiProvider.cs`):** Contain the core logic
    for interacting with specific AI service APIs (SDK usage, direct HTTP calls, authentication,
    request/response formatting). They are primarily used internally by the Provider Factories.
-   **`IAiProvider.cs`:** Defines the basic contract implemented by the Base Providers.

## 3. Testing Strategy

A combination of unit and integration tests ensures the reliability of the AI provider system.

### 3.1. Approach

-   **Unit Testing (`ChattyMcChatface.Tests.Unit`):**
    -   Focuses on isolating and testing individual components: Provider Factories, Model Classes,
        `AiFallbackUtil`, and the handler logic within `PersonaService`.
    -   Utilizes mocking frameworks (Moq) to simulate dependencies like `IConfiguration`, `ILogger`,
        `IHttpClientFactory`, and the base providers themselves.
    -   Ensures that components function correctly in isolation (e.g., factories create delegates
        that call the correct provider method, fallback logic handles different scenarios,
        `PersonaService` handler switches correctly).
-   **Integration Testing (`ChattyMcChatface.Tests.Integration`):**
    -   Verifies end-to-end functionality and direct API connectivity.
    -   Uses **real API calls** with actual credentials managed via .NET User Secrets.
    -   Includes tests for `PersonaService` (end-to-end flow) and individual providers (API
        connectivity).
    -   These tests are typically marked to be skipped in CI environments due to their reliance on
        external services and secrets.
-   **Frameworks:** xUnit, FluentAssertions, Moq.

### 3.2. Test Project Structure

-   Unit Tests: `dotnet_server/ChattyMcChatface.Tests.Unit`
-   Integration Tests: `dotnet_server/ChattyMcChatface.Tests.Integration`
-   Tests are organized by component type within these projects (e.g., `AI/Factories`,
    `AI/ModelClasses`, `Services`).

## 4. Configuration & Secrets Management

API keys and other sensitive configuration required for AI providers are managed differently for
local development/testing and deployment.

### 4.1. Local Integration Testing

-   **.NET User Secrets:** This is the recommended approach for managing API keys locally for the
    `ChattyMcChatface.Tests.Integration` project.
-   **Initialization:** Use `dotnet user-secrets init` in the integration test project directory.
-   **Setting Secrets:** Use `dotnet user-secrets set "Provider:KeyName" "KeyValue"` (e.g.,
    `dotnet user-secrets set "Anthropic:ApiKey" "your-api-key"`).
-   **Specific Provider Notes:**
    -   **Azure OpenAI:** Requires nested keys for each deployment (e.g.,
        `AzureOpenAI:Thrivify:ApiKey`, `AzureOpenAI:Thrivify:Endpoint`).
    -   **Vertex AI:**
        -   Requires the _entire JSON content_ of the service account key file set to the
            `VertexAI:KeyJsonContent` secret. Use single quotes to handle special characters in the
            shell: `dotnet user-secrets set 'VertexAI:KeyJsonContent' '{...json_content...}'`.
        -   Ensure the `private_key` within the JSON has properly formatted newlines (`\n`). The
            `VertexAiProvider` includes logic to attempt fixing common formatting issues, but
            correct initial formatting is best.
        -   Requires the Google Cloud region set via
            `dotnet user-secrets set "VertexAI:Location" "your-region"`.
-   **Reference:** See `secrets.example.json` in the `ChattyMcChatface.Api` project for key names.

### 4.2. Unit Testing

-   Unit tests **must not** rely on User Secrets or real configuration values.
-   Use `Mock<IConfiguration>` to provide necessary dummy values during test setup to satisfy
    provider/factory constructors. Key examples include `Anthropic:ApiKey`,
    `VertexAI:KeyJsonContent`, `VertexAI:Location`, `AzureOpenAI:Thrivify:ApiKey`,
    `AzureOpenAI:Thrivify:Endpoint`, etc. Ensure the keys used in the mock setup exactly match those
    read by the provider constructors, as providers will now throw an `InvalidOperationException` if
    a required key is missing.
-   When mocking AI Model classes (e.g., `AzureAiModels`, `OpenAiModels`) that depend on
    `IServiceProvider` in their constructors, ensure the mock `IServiceProvider` is configured using
    `.Setup()` to return mock instances of required dependencies, particularly `ILogger<T>` (e.g.,
    `ILogger<AzureAiProvider>`, `ILogger<OpenAiProvider>`). Instantiate the `Mock<ModelClass>`
    itself passing only the configured `IServiceProvider` mock object (e.g.,
    `new Mock<AzureAiModels>(mockServiceProvider.Object)`).

## 5. Key Implementation Details & Considerations

### 5.1. Provider Implementations

-   **OpenAI:** Standard implementation using the official OpenAI SDK.
-   **Claude (Direct):** Uses direct HTTP calls to the Anthropic Claude API.
-   **Azure OpenAI:** Uses the official Azure OpenAI SDK.
-   **Gemini:** Uses the `Google.Ai.Generative.Gemini` SDK.
-   **Vertex AI (Claude):** Uses direct HTTP calls to the Vertex AI endpoint for Claude models
    (`publishers/anthropic`). This requires special handling:
    -   Service account JSON authentication (see Secrets section).
    -   Correct endpoint structure (`.../publishers/anthropic/models/{modelId}:rawPredict`).
    -   Specific request format (`anthropic_version`, message structure).
    -   Custom response parsing.

### 5.2. Adding/Modifying Providers

When adding a new AI provider or modifying an existing one, ensure the following steps are taken:

1.  **Implement Base Provider:** Create or modify the class inheriting `IAiProvider` (e.g.,
    `NewAiProvider.cs`) containing the core API interaction logic.
2.  **Define Model Constants:** Add relevant model identifier constants to `AiModels.cs`.
3.  **Create Factory:** Implement a static `NewAiProviderFactory.cs` to create the `Func` delegates,
    encapsulating `NewAiProvider` instantiation.
4.  **Create Model Class:** Implement a singleton `NewAiModels.cs` class, injecting dependencies
    (like `IConfiguration`, `ILogger`, `IHttpClientFactory` if needed) and using the factory to
    initialize `Func` properties for each model variant. Register this singleton in DI (`Program.cs`
    and `IntegrationTestFixture.cs`).
5.  **Update `PersonaService`:**
    -   Inject the new `NewAiModels` singleton.
    -   Update the `switch` statement within the handler function passed to
        `AiFallbackUtil.GetWithFallbackAsync` to call the appropriate delegate from the new model
        class based on model IDs.
6.  **Add Configuration:** Define necessary configuration keys (API keys, endpoints) and update
    `secrets.example.json`.
7.  **Implement Unit Tests:**
    -   Add tests for the new Factory (`NewAiProviderFactoryTests.cs`), mocking dependencies.
    -   Add tests for the new Model Class (`NewAiModelsTests.cs`), mocking dependencies.
    -   Update `PersonaServiceTests.cs` to mock the new `NewAiModels` class and add test cases
        covering its usage. Ensure all necessary configuration mocks are added (using the correct
        keys) and that the mock `IServiceProvider` (if used by the Model class) is properly
        configured via `.Setup()` to provide required loggers and other dependencies.
8.  **Implement Integration Tests:**
    -   Add `NewAiProviderIntegrationTests.cs` to verify direct API connectivity.
    -   Update `PersonaServiceIntegrationTests.cs` with test cases using the new provider's models.
    -   Ensure User Secrets are configured locally for these tests.

## 6. Quick Reference: Build and Test Commands

_(Located in `dotnet_server` directory)_

-   **Build Solution:** `dotnet build ChattyMcChatface.sln`
-   **Run All Tests:** `dotnet test ChattyMcChatface.sln`
-   **Run Unit Tests Only:**
    `dotnet test ChattyMcChatface.Tests.Unit/ChattyMcChatface.Tests.Unit.csproj`
-   **Run Integration Tests Only:**
    `dotnet test ChattyMcChatface.Tests.Integration/ChattyMcChatface.Tests.Integration.csproj`
-   **Manage User Secrets (in `ChattyMcChatface.Tests.Integration` dir):**
    -   `dotnet user-secrets init`
    -   `dotnet user-secrets set "Key" "Value"`
    -   `dotnet user-secrets list`
