# ChattyMcChatface - AI Provider Testing Plan (Revised)

## 1. Introduction

**Purpose:** This document outlines the plan for testing the refactored AI provider implementation
within the ChattyMcChatface .NET project. The refactoring introduced code-based model instantiation
and a new fallback utility.

**Goal:** To verify that: 1. Individual AI provider components (factories, model classes) function
correctly. 2. The new fallback utility (`AiFallbackUtil`) correctly handles success and failure
scenarios across different models. 3. The end-to-end flow through `PersonaService` successfully
generates responses using the new structure and fallback logic. 4. Direct API connectivity for each
provider/model combination remains functional (via integration tests).

**Scope:** This plan covers unit and integration tests for the refactored AI system. Integration
tests will make **real API calls**.

## 2. System Overview (Refactored)

The core functionality involves generating text responses using specific AI models, orchestrated by
`PersonaService` which utilizes a fallback utility (`AiFallbackUtil`) and model-specific delegates.

### 2.1. Core Components

-   **`AiModels.cs`:** Defines constants for all supported AI model identifiers, including special formatting
    for Vertex AI models (e.g., `Claude37SonnetVertex = "claude-3-7-sonnet@20250219"` uses the @ symbol for
    Vertex AI's versioning system).
-   **Provider Factories (e.g., `OpenAiProviderFactory.cs`):** Static classes responsible for
    creating `Func<string, List<ChatMessageDto>, Task<string?>>` delegates configured for a specific
    provider and model ID. They encapsulate the instantiation of the underlying provider
    (`OpenAiProvider`, `AzureAiProvider`, etc.).
-   **Model Classes (e.g., `OpenAiModels.cs`):** Singleton classes injected via DI. They use the
    provider factories during initialization to create and hold pre-configured `Func` delegates for
    each specific model variant (e.g., `OpenAiModels.Gpt4oLatest`).
-   **`AiFallbackUtil.cs`:** Contains the static `GetWithFallbackAsync` method. It takes a global
    priority list, a preferred model ID, and a handler function. It iterates through models, calling
    the handler until success or exhaustion.
-   **`PersonaService.cs`:** Injects the model classes (e.g., `OpenAiModels`). Its
    `GenerateResponseAsync` method calls `AiFallbackUtil.GetWithFallbackAsync`, passing the global
    priority list, the persona's preferred model ID, and a handler function. The handler uses a
    `switch` statement on the model ID to invoke the correct delegate from the injected model
    classes (e.g., `_openAiModels.Gpt4oLatest(...)`).
-   **Base Providers (e.g., `OpenAiProvider.cs`):** Still exist and contain the core logic for
    interacting with specific AI service APIs. They are now primarily used internally by the
    factories.
-   **`IAiProvider.cs`:** Still defines the basic contract, but is less central to the application
    flow, mainly used by the base providers.

### 2.2. Functionality Under Test

1.  **Factory Correctness:** Do factories create delegates that correctly invoke the underlying
    provider with the right model ID?
2.  **Model Class Initialization:** Do model classes correctly initialize all their `Func`
    properties using the factories and constants?
3.  **Fallback Logic:** Does `AiFallbackUtil.GetWithFallbackAsync` correctly handle preferred model
    success, fallback success, and complete failure scenarios?
4.  **Handler Logic (`PersonaService`):** Does the handler function passed to `GetWithFallbackAsync`
    correctly `switch` on the model ID and call the appropriate model-specific delegate?
5.  **End-to-End Flow:** Does `PersonaService.GenerateResponseAsync` successfully produce a response
    using the entire new mechanism?
6.  **API Connectivity:** Can each underlying provider (`OpenAiProvider`, etc.) still successfully
    communicate with its live API for key models?

## 3. Testing Strategy

### 3.1. Approach

-   **Unit Testing:** Focus on isolating and testing individual components (factories, model
    classes, fallback utility, `PersonaService` handler logic) using mocks.
-   **Integration Testing:** Verify end-to-end functionality and direct API connectivity using real
    credentials and live API calls. Mark these tests to be skipped in CI environments.
-   **Framework:** xUnit and FluentAssertions. Moq or NSubstitute for mocking.

### 3.2. Test Project Structure

-   Use the existing or create a new test project: `dotnet_server/ChattyMcChatface.Tests.Unit`.
-   Use the existing or create a new test project:
    `dotnet_server/ChattyMcChatface.Tests.Integration`.
-   Organize tests by component type (e.g., `AI/Factories`, `AI/ModelClasses`, `AI/Utils`,
    `Services`).

## 4. Handling Secrets (API Keys)

-   The **.NET Secret Manager** approach described previously remains the recommended solution for
    handling API keys during local integration testing.
-   Ensure User Secrets are initialized for the `ChattyMcChatface.Tests.Integration` project and
    populated with the necessary API keys.
-   For Vertex AI, configure the `Vertex:ServiceAccountJson` secret using `dotnet user-secrets set` to
    contain the _entire JSON content_ of your downloaded service account key. Also set `Vertex:Region` 
    to the appropriate Google Cloud region (e.g., "europe-west1"). Ensure this JSON content is stored 
    securely and **not** checked into source control. The service account key must have properly 
    formatted newlines in the private key.
-   For Azure OpenAI, secrets should be set for each deployment under test, following the nested
    structure, e.g., `dotnet user-secrets set AzureOpenAI:Thrivify:ApiKey YOUR_KEY` and
    `dotnet user-secrets set AzureOpenAI:Thrivify:Endpoint YOUR_ENDPOINT`.

## 5. Test Implementation Details

### 5.1. Unit Tests

-   **Factories (`OpenAiProviderFactoryTests.cs`, etc.):**
    -   Mock `IConfiguration`, `ILogger<Provider>`, `IHttpClientFactory` (for Gemini).
    -   Mock the underlying provider (`Mock<OpenAiProvider>`).
    -   Call the factory method (e.g., `CreateOpenAiCompletionProvider`).
    -   Invoke the returned `Func` delegate.
    -   Verify that the mocked provider's `GetCompletionAsync` was called once with the expected
        `modelId`.
-   **Model Classes (`OpenAiModelsTests.cs`, etc.):**
    -   Mock `IServiceProvider` and the dependencies it resolves (`IConfiguration`,
        `ILogger<Provider>`, `IHttpClientFactory`).
    -   Instantiate the model class (e.g., `new OpenAiModels(mockServiceProvider.Object)`).
    -   Assert that the `Func` properties (e.g., `models.Gpt4oLatest`) are not null.
    -   _(Optional/Advanced):_ Could potentially mock the factory static methods to verify they are
        called with correct parameters during instantiation, though this might be overly complex.
        Focus on verifying the properties are initialized.
-   **Fallback Utility (`AiFallbackUtilTests.cs`):**
    -   Create a mock `ILogger`.
    -   Define test priority lists and preferred models.
    -   Create mock handler functions (`Func<string, Task<string?>>`) that simulate:
        -   Success on the first (preferred) model.
        -   Failure on preferred, success on fallback.
        -   Failure on all models (throwing exceptions or returning null/empty).
    -   Call `AiFallbackUtil.GetWithFallbackAsync` with different scenarios.
    -   Assert the correct result is returned or the expected `AggregateException` is thrown.
    -   Verify logger calls for attempts, success, and failures.
-   **`PersonaServiceTests.cs`:**
    -   Mock all dependencies: `AppDbContext`, `IPersonaConfigService`, `ILogger<PersonaService>`,
        `INotificationService`, and all model classes (`Mock<OpenAiModels>`, `Mock<AzureAiModels>`,
        etc.).
    -   Set up mock methods for the model classes (e.g.,
        `mockOpenAiModels.Setup(m => m.Gpt4oLatest(It.IsAny<string>(), It.IsAny<List<ChatMessageDto>>())).ReturnsAsync("Mock Response");`).
    -   Set up `IPersonaConfigService` to return a test `PersonaConfig`.
    -   Call `_personaService.GenerateResponseAsync`.
    -   Verify that the correct model-specific function (e.g.,
        `mockOpenAiModels.Verify(m => m.Gpt4oLatest(...), Times.Once());`) was called based on the
        `preferredModelId` and fallback logic (this implicitly tests the handler's switch
        statement).
    -   Verify `_dbContext.ChatMessages.Add` and `SaveChangesAsync` were called.
    -   Verify `_notificationService.SendMessageToGroupAsync` was called.

### 5.2. Integration Tests

-   **Configure Test Secrets:** Before running the integration tests, ensure the `secrets.json` file
    is properly configured for the integration test project (once created). Use the
    `dotnet user-secrets set` command to add the necessary keys (including API keys, the
    `VertexAI:KeyJsonContent`, and nested Azure keys like `AzureOpenAI:Thrivify:ApiKey`,
    `AzureOpenAI:Thrivify:Endpoint`, etc.), referencing the `secrets.example.json` template. This
    step should be performed after the test project is created and before the tests are executed.

-   **Test Fixture (`IntegrationTestFixture.cs`):**
    -   Essential for setting up `IConfiguration` (including User Secrets) and a real
        `IServiceProvider` with all services registered as in `Program.cs`.
    -   Ensure it correctly loads configuration including User Secrets where the
        `VertexAI:KeyJsonContent` would be defined.
-   **End-to-End `PersonaServiceIntegrationTests.cs`:**
    -   Use the fixture to get an instance of `IPersonaService`.
    -   Seed test data (chatroom, user, persona config with a specific `preferredModelId`).
    -   Call `_personaService.GenerateResponseAsync`.
    -   Assert that a non-empty response message was added to the context/database.
    -   Use `[Fact(Skip = "...")]` to skip in CI.
    -   Test with different `preferredModelId` values to cover various providers.
-   **Provider-Level Connectivity Tests (Optional):**
    -   Files like `OpenAiProviderIntegrationTests.cs`, etc.
    -   Use the fixture to get `IConfiguration` and `ILogger`.
    -   Directly instantiate the provider (e.g.,
        `new OpenAiProvider(_fixture.Configuration, _fixture.Logger)`).
    -   Call `GetCompletionAsync` with a specific model ID and basic input.
    -   Assert non-null/non-empty response.
    -   For Vertex AI tests, ensure the `Vertex:ServiceAccountJson` secret contains the valid service
        account JSON and `Vertex:Region` is set correctly. The VertexAiProvider uses a direct HTTP call to 
        the Vertex AI Claude API endpoint with the `rawPredict` suffix for Claude models. This implementation
        handles the service account authentication and proper formatting of requests.
    -   Use `[Fact(Skip = "...")]` to skip in CI.

#### 5.2.1. Implementation Status

The AI providers have been successfully implemented with integration tests to verify functionality:

-   **OpenAI**: Standard implementation using the OpenAI SDK with API key authentication.
-   **Claude (Direct)**: Implementation using direct HTTP calls to the Claude API with API key authentication.
-   **Azure OpenAI**: Implementation using the Azure OpenAI SDK with endpoint and API key authentication.
-   **Gemini**: Implementation using the Google.Ai.Generative.Gemini SDK with API key authentication.
-   **Vertex AI**: Implementation using direct HTTP calls to the Vertex AI Claude API endpoint with service account authentication. 
    This provider required special handling for:
    - Service account JSON with properly formatted newlines in the private key
    - Using the correct endpoint format with publishers/anthropic for Claude models
    - Using the `:rawPredict` endpoint suffix rather than the standard `:predict`
    - Formatting message content with the required `role` and `content` structure
    - Including the `anthropic_version` field in requests
    - Proper extraction of text from the response format

Integration tests have been created that:
1. Test basic question answering
2. Test conversation continuity
3. Test reasoning capabilities

Each test verifies that the provider can properly:
- Authenticate with the API
- Format requests correctly
- Receive and parse responses
- Maintain conversation context

### 5.2.2. Example Test Structure (from SoundLikeUs project)

The integration tests in the `soundlikeus` project
(`/Users/ryanpfister/Dev/soundlikeus/soundlikeus-api/test/integration/ai-providers`) served as a
structural guide for organizing the .NET integration tests by provider. The implemented test files include:

-   `AzureAiProviderIntegrationTests.cs`
-   `ClaudeProviderIntegrationTests.cs`
-   `GeminiProviderIntegrationTests.cs`
-   `OpenAiProviderIntegrationTests.cs`
-   `VertexAiProviderIntegrationTests.cs`

Each provider has tests covering the same basic capabilities, allowing for comparison of results
across different AI models and ensuring the refactored structure operates correctly.

## 6. Challenges and Lessons Learned

### 6.1. Vertex AI Implementation Challenges

The Vertex AI implementation was particularly challenging due to several factors:

1. **Service Account Authentication**: Unlike the other providers that use simple API keys, Vertex AI requires a service account JSON file with a properly formatted private key.

2. **Correct Endpoint Structure**: Vertex AI has a unique endpoint structure that varies based on the publisher. For Claude models, we had to use `publishers/anthropic` rather than `publishers/google`.

3. **Request Format Differences**: The request format for Claude via Vertex AI is different from direct Claude API calls:
   - Requires `anthropic_version` field set to "vertex-2023-10-16"
   - Uses the `:rawPredict` endpoint instead of `:predict`
   - Requires a specifically structured message format

4. **Response Format**: The JSON response format from Claude via Vertex AI differs from other providers, requiring special handling to extract the text content.

5. **Documentation Gaps**: The exact format requirements were not clearly documented, requiring experimentation and comparing with working implementations in other languages.

### 6.2. Solution Approach

The solution approach included:

1. Creating test applications to isolate and debug the authentication issues
2. Fixing the service account JSON format, ensuring proper newline handling in the private key
3. Implementing direct HTTP calls rather than using the SDK's Predict method
4. Using the successful Node.js implementation from another project as a reference
5. Adding extensive logging to track request and response formats
6. Creating robust integration tests that verify different aspects of functionality

### 6.3. Execution

-   Ensure API keys are configured in User Secrets for the integration test project.
-   Run unit tests frequently during development (`dotnet test` filtered to the unit test project).
-   Run integration tests locally before merging (`dotnet test` filtered to the integration test
    project or using categories/traits).
-   For Vertex AI specifically, always ensure that the service account JSON has properly formatted
    newlines in the private key section.

## 7. Quick Reference: Build and Test Commands

For easy reference, here are the common commands for building and testing the project:

### 7.1. Building the Project

```bash
# Build the entire solution
cd /path/to/chatty-mcchatface/dotnet_server
dotnet build ChattyMcChatface.sln

# Build a specific project
cd /path/to/chatty-mcchatface/dotnet_server
dotnet build ChattyMcChatface.Api/ChattyMcChatface.Api.csproj
```

### 7.2. Running Tests

```bash
# Run all tests
cd /path/to/chatty-mcchatface/dotnet_server
dotnet test ChattyMcChatface.sln

# Run only unit tests
cd /path/to/chatty-mcchatface/dotnet_server
dotnet test ChattyMcChatface.Tests.Unit/ChattyMcChatface.Tests.Unit.csproj

# Run only integration tests
cd /path/to/chatty-mcchatface/dotnet_server
dotnet test ChattyMcChatface.Tests.Integration/ChattyMcChatface.Tests.Integration.csproj

# Run a specific test by name
dotnet test --filter "FullyQualifiedName=ChattyMcChatface.Tests.Integration.AI.Providers.VertexAiProviderIntegrationTests.GetCompletionAsync_SimpleTest_ReturnsExpectedResponse"
```

### 7.3. Managing User Secrets

```bash
# Initialize user secrets for a project
cd /path/to/chatty-mcchatface/dotnet_server/ChattyMcChatface.Tests.Integration
dotnet user-secrets init

# Set a single secret
dotnet user-secrets set "Claude:ApiKey" "your-api-key-here"

# Set the Vertex AI service account JSON (use single quotes to handle special characters)
dotnet user-secrets set 'Vertex:ServiceAccountJson' '{"type":"service_account","project_id":"...","private_key":"...",...}'

# Set the Vertex AI region
dotnet user-secrets set "Vertex:Region" "europe-west1"

# List all secrets
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove "KeyToRemove" 

# Clear all secrets
dotnet user-secrets clear
```

### 7.4. Running the API

```bash
# Run the API project
cd /path/to/chatty-mcchatface/dotnet_server
dotnet run --project ChattyMcChatface.Api/ChattyMcChatface.Api.csproj

# Run with watch for development (auto-reload on changes)
cd /path/to/chatty-mcchatface/dotnet_server
dotnet watch run --project ChattyMcChatface.Api/ChattyMcChatface.Api.csproj
```
