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

-   **`AiModels.cs`:** Defines constants for all supported AI model identifiers.
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
-   For Vertex AI, configure the `VertexAI:KeyFilePath` secret using `dotnet user-secrets set` to
    point to the location of your downloaded service account key JSON file on your local machine.
    Ensure this key file is stored securely and **not** checked into source control.

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
    `dotnet user-secrets set` command to add the necessary keys (including API keys and the
    `VertexAI:KeyFilePath`), referencing the `secrets.example.json` template. This step should be
    performed after the test project is created and before the tests are executed.

-   **Test Fixture (`IntegrationTestFixture.cs`):**
    -   Essential for setting up `IConfiguration` (including User Secrets) and a real
        `IServiceProvider` with all services registered as in `Program.cs`.
    -   Ensure it correctly loads configuration including User Secrets where the
        `VertexAI:KeyFilePath` would be defined.
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
    -   For Vertex AI tests, ensure the `VertexAI:KeyFilePath` secret is correctly set for the test
        to pass.
    -   Use `[Fact(Skip = "...")]` to skip in CI.

#### 5.2.1. Example Test Structure (from SoundLikeUs project)

The integration tests in the `soundlikeus` project
(`/Users/ryanpfister/Dev/soundlikeus/soundlikeus-api/test/integration/ai-providers`) can serve as a
structural guide for organizing the .NET integration tests by provider. Key files include:

-   `azure-provider.integration.test.ts`
-   `claude-provider.integration.test.ts`
-   `gemini-provider.integration.test.ts`
-   `openai-provider.integration.test.ts`
-   `vertex-provider.integration.test.ts`

**Note:** While the structure (organizing tests by provider, potentially using helper functions for
common test logic) can be adapted, the specific functionalities tested in `soundlikeus` (e.g., story
generation, character creation, image description) are different from the chat completion focus of
`ChattyMcChatface`. The .NET tests should focus on verifying the `GetCompletionAsync` functionality
for each provider/model via the refactored structure (model classes, fallback utility) and direct
provider connectivity.

## 6. Execution

-   Ensure API keys are configured in User Secrets for the integration test project.
-   Run unit tests frequently during development (`dotnet test` filtered to the unit test project).
-   Run integration tests locally before merging (`dotnet test` filtered to the integration test
    project or using categories/traits).
