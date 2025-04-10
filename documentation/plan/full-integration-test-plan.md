# AI Persona Integration Testing Implementation Plan

## Overview

This document outlines the detailed implementation plan for comprehensive integration testing of the
AI persona functionality in ChattyMcChatface. These tests aim to validate the end-to-end flow using
live AI providers, actual persona configurations from `personas.json`, and a real database instance
(e.g., SQLite).

The implementation follows the requirements specified in `documentation/plan.md` and focuses on
Scenario 1: verifying that a persona correctly processes a new message, generates a response, saves
it, triggers real-time updates, and updates its read status.

## Implementation Steps

### 1. Refactor `IntegrationTestFixture.cs`

**Goal:** Set up the core testing infrastructure using `WebApplicationFactory` for in-memory API
hosting, configure DI for testing (including DB and mocks), and provide helper methods.

**Actions:**

-   Modify `IntegrationTestFixture` to inherit from `WebApplicationFactory<Program>` (where
    `Program` is the entry point class of `ChattyMcChatface.Api`).
-   Override `ConfigureWebHost` to:
    -   Replace the existing `AppDbContext` registration with one that uses the shared in-memory
        SQLite connection string (`DataSource=TestDatabase;Mode=Memory;Cache=Shared`).
    -   Register a mock `INotificationService` using Moq, replacing the real
        `SignalRNotificationService`.
    -   Configure test authentication (e.g., using `TestAuthHandler`) to allow creating
        authenticated `HttpClient` instances.
-   Ensure the database connection is opened and migrations are applied within the fixture's setup
    (potentially in the constructor or a shared initialization method).
-   Add a `ResetDatabaseAsync` method to clear relevant tables (`ChatMessages`, `Chatrooms`,
    `Users`, `LastReads`) before each test run to ensure isolation. This will likely involve getting
    an `AppDbContext` instance and executing `DELETE FROM` or `TRUNCATE` commands.
-   Add asynchronous helper methods for seeding data:
    -   `SeedUserAsync(bool isPersona = false)`
    -   `SeedChatroomAsync(List<User> users, int? personaUserId)`
    -   `SeedMessagesAsync(int chatroomId, int userId, int count, DateTime startDate)`
    -   `SeedLastReadAsync(int chatroomId, int userId, DateTime lastReadDate)`
-   Expose the mock `INotificationService` instance and a method to create an authenticated
    `HttpClient`.

### 2. Implement Test Setup in `PersonaServiceIntegrationTests.cs`

**Goal:** Prepare the specific state required for Scenario 1 before executing the test logic.

**Actions:**

-   Update `PersonaServiceIntegrationTests` to use the refactored
    `IClassFixture<IntegrationTestFixture>`.
-   Inject `IntegrationTestFixture` into the constructor.
-   Create a new test method: `[Fact] public async Task PersonaRespondsToNewMessage_Scenario1()`.
-   Inside the test method:
    -   Get an `HttpClient` from the fixture (already authenticated).
    -   Get the mock `INotificationService` from the fixture.
    -   Get an `IServiceScopeFactory` from the fixture's services to resolve scoped services like
        `AppDbContext` within the test.
    -   Call `await _fixture.ResetDatabaseAsync();` to clear the DB.
    -   Use the fixture's seeding helpers to:
        -   Create `testUser`.
        -   Create `personaUser` (with `IsPersona = true`, e.g., ID 1001).
        -   Create `chatroom` including both users, setting `PersonaUserId`. Store `chatroomId`.
        -   Seed > 20 `ChatMessage`s, alternating users, ensuring the last one is from `testUser`.
            Store the date of the second-to-last message (`lastPersonaMessageDate`) and the last
            message (`lastUserMessageDate`).
        -   Seed `LastRead` for `personaUser` with `lastPersonaMessageDate`.
        -   Seed `LastRead` for `testUser` with `lastUserMessageDate`.

### 3. Implement API Call

**Goal:** Simulate the user sending a new message via the API.

**Actions:**

-   Create a `ChatMessageDto` (`newUserMessageDto`) representing a new message from `testUser`.
-   Use the `HttpClient` to send a `POST` request to `/api/chatrooms/{chatroomId}/chats` with
    `newUserMessageDto` as the body.
-   Assert that the response status code is `201 Created`.

### 4. Implement Wait and Database Verification (Persona Message)

**Goal:** Verify that the `PersonaService` processed the message and saved the AI's response to the
database.

**Actions:**

-   Add a `await Task.Delay(...)` (e.g., 5-10 seconds, adjustable) to allow asynchronous processing.
-   Create a service scope using `IServiceScopeFactory`.
-   Resolve `AppDbContext` from the scope.
-   Query the `ChatMessages` table for messages in the `chatroomId` created after
    `lastUserMessageDate` and belonging to `personaUser.Id`.
-   Assert that exactly one such message exists. Store this `personaResponseMessage`.
-   Assert that `personaResponseMessage.Text` is not null or empty.

### 5. Implement `LastRead` Verification

**Goal:** Verify that the persona's `LastRead` status was updated correctly.

**Actions:**

-   Using the same scoped `AppDbContext` (or a new one), query the `LastReads` table for the record
    matching `chatroomId` and `personaUser.Id`.
-   Assert that the record exists.
-   Assert that its `LastReadDate` is equal to `personaResponseMessage.Date`.

### 6. Implement Notification Verification

**Goal:** Verify that the SignalR notification was triggered correctly via the mocked service.

**Actions:**

-   Use the injected mock `INotificationService` instance.
-   Use `Mock.Verify()` to assert that `SendMessageToGroupAsync` was called exactly once.
-   Verify the arguments passed to `SendMessageToGroupAsync`:
    -   `groupId` matches `chatroomId.ToString()`.
    -   `chatroomId` matches the integer `chatroomId`.
    -   The `ChatMessageDto` argument matches the details of the `personaResponseMessage` saved in
        the database (ID, Text, Date, UserId, UserFirstName, UserLastName, ChatroomId,
        Role=Assistant).

## Implementation Sequence

The implementation will proceed in the following order:

1. First, refactor the `IntegrationTestFixture.cs` to provide the core testing infrastructure.
2. Next, implement the test setup and seeding helpers.
3. Then, implement the actual test method with API call, database verification, LastRead
   verification, and notification verification.

Each step will be implemented incrementally, with verification after each step to ensure correctness
before proceeding to the next step.

## Debugging Notes for Scenario 1

During the implementation and execution of
`PersonaServiceIntegrationTests.PersonaRespondsToNewMessage_Scenario1`, several issues were
encountered and resolved:

1.  **Initial 500 Internal Server Error (Missing `IHubContext<ChatHub>`)**:

    -   **Symptom:** The test failed at the API call (`client.PostAsync(...)`) assertion, expecting
        status 201 but receiving 500. Test output showed
        `System.InvalidOperationException: Unable to resolve service for type 'Microsoft.AspNetCore.SignalR.IHubContext<ChatHub>' while attempting to activate 'ChattyMcChatface.Api.Controllers.ChatroomsController'`.
    -   **Cause:** The SignalR services, required by `ChatroomsController`, were not registered in
        the test server's DI container within `IntegrationTestFixture.cs`.
    -   **Fix:** Added `services.AddSignalR();` inside the `ConfigureServices` lambda in
        `dotnet_server/ChattyMcChatface.Tests.Integration/IntegrationTestFixture.cs`.

2.  **Second 500 Internal Server Error (Missing `IPersonaConfigService`)**:

    -   **Symptom:** After fixing the `IHubContext` issue, the test still failed with a 500 error.
        Test output showed
        `System.InvalidOperationException: Unable to resolve service for type 'ChattyMcChatface.Core.Services.IPersonaConfigService' while attempting to activate 'ChattyMcChatface.Api.Controllers.ChatroomsController'`.
    -   **Cause:** The `IPersonaConfigService` (and likely `IPersonaService`), also dependencies of
        `ChatroomsController`, were not registered in the test DI container.
    -   **Fix:** Added `services.AddSingleton<IPersonaConfigService, PersonaConfigService>();` and
        `services.AddScoped<IPersonaService, PersonaService>();` inside the `ConfigureServices`
        lambda in `dotnet_server/ChattyMcChatface.Tests.Integration/IntegrationTestFixture.cs`.

3.  **Third 500 Internal Server Error (Missing AI Models)**:
    -   **Symptom:** After fixing the previous DI issues, the test failed again with a 500 error.
        Test output showed
        `System.InvalidOperationException: Unable to resolve service for type 'ChattyMcChatface.Core.Services.AI.OpenAI.OpenAiModels' while attempting to activate 'ChattyMcChatface.Core.Services.PersonaService'`.
    -   **Cause:** The `PersonaService` depends on concrete AI model classes (`OpenAiModels`,
        `AzureAiModels`, etc.), which were not registered in the test DI container. These classes
        themselves resolve dependencies like `IConfiguration` and `ILogger` via `IServiceProvider`
        in their constructors.
    -   **Fix:** Added singleton registrations for all required AI model classes (`OpenAiModels`,
        `AzureAiModels`, `ClaudeModels`, `GeminiModels`, `VertexAiModels`) inside the
        `ConfigureServices` lambda in
        `dotnet_server/ChattyMcChatface.Tests.Integration/IntegrationTestFixture.cs`. Also ensured
        necessary `using` statements were present.

After these DI registrations were added to `IntegrationTestFixture.cs`, the
`PersonaRespondsToNewMessage_Scenario1` test passed successfully.

## Additional Test Scenarios

Beyond Scenario 1, the following integration tests should be considered to ensure robustness:

### Scenario 2: Persona Ignores Own Messages

**Objective:** Verify the persona does not enter a loop by responding to its own messages.
**Pre-conditions:**

1. Seed DB similar to Scenario 1, but ensure the _last_ message is from the `personaUser`.
2. Set `LastRead` for `personaUser` to the timestamp of its own last message. **Test Steps &
   Validation:**
3. Trigger processing (e.g., manually call a hypothetical endpoint or wait for a background job if
   applicable, although the current flow seems triggered only by user messages). Alternatively, send
   a _new_ message from the `personaUser` via the API (if allowed/possible).
4. Wait for a significant duration (longer than typical response time).
5. Query `ChatMessages` table.
6. **Validation:** Assert that _no new_ message from `personaUser` was added after the trigger/wait
   period.

### Scenario 3: Persona Handles Empty History

**Objective:** Verify the persona can generate a response when a chatroom is new and has no message
history. **Pre-conditions:**

1. Seed `testUser` and `personaUser`.
2. Seed a new `chatroom` containing both users, with `PersonaUserId` set.
3. Seed `LastRead` for both users (e.g., to the chatroom creation time or slightly before the first
   message). **Test Steps & Validation:**
4. Create `newUserMessageDto` (the first message in the chatroom).
5. Call `POST /api/chatrooms/{chatroomId}/chats`. Assert 201.
6. Wait for async processing.
7. Query `ChatMessages`.
8. **Validation:** Assert a new message from `personaUser` exists.
9. **Validation:** Verify `LastRead` for `personaUser` is updated.
10. **Validation:** Verify `mockNotificationService` was called.

### Scenario 4: Persona Uses Correct History Limit (MaxHistoryMessages)

**Objective:** Indirectly verify that the persona considers only the most recent
`MaxHistoryMessages` (currently 20) when generating a response. **Pre-conditions:**

1. Seed `testUser` and `personaUser`.
2. Seed `chatroom` with `PersonaUserId`.
3. Seed exactly `MaxHistoryMessages` (e.g., 20) messages, alternating users, with distinct content
   in the first few vs. the last few. Ensure the last message is from `testUser`.
4. Seed `LastRead` appropriately. **Test Steps & Validation:**
5. Create `newUserMessageDto` with content that relates specifically to the _last_ few messages in
   the history, not the first few.
6. Call `POST /api/chatrooms/{chatroomId}/chats`. Assert 201.
7. Wait for async processing.
8. Query `ChatMessages` for the `personaResponseMessage`.
9. **Validation:** Assert `personaResponseMessage` exists and its text is not empty.
10. **Validation (Qualitative):** Manually inspect the `personaResponseMessage.Text`. Does it seem
    contextually relevant to the `newUserMessageDto` and the _end_ of the seeded history, rather
    than the beginning? (Note: Automated assertion is difficult here).
11. **Validation:** Verify `LastRead` update and notification call.

### Scenario 5: Persona Fallback Mechanism

**Objective:** Verify the `AiFallbackUtil` correctly switches to a backup AI model if the preferred
one fails. **Pre-conditions:**

1. Identify the preferred model for the test persona (e.g., `gemini-2.5-pro-preview-03-25` in the
   logs) and a valid fallback model (e.g., `chatgpt-4o-latest`).
2. **Crucially:** Configure User Secrets for the test project such that the API key/credentials for
   the _preferred_ model are _invalid_ or _missing_, but the credentials for the _fallback_ model
   are _valid_.
3. Seed DB similar to Scenario 1. **Test Steps & Validation:**
4. Create `newUserMessageDto`.
5. Call `POST /api/chatrooms/{chatroomId}/chats`. Assert 201.
6. Wait for async processing.
7. Query `ChatMessages` for `personaResponseMessage`.
8. **Validation:** Assert `personaResponseMessage` exists and its text is not empty (indicating a
   successful response was generated despite the preferred model failing).
9. **Validation (Optional/Advanced):** Check application logs generated during the test run (if
   possible within the test framework) for log messages from `AiFallbackUtil` indicating the
   fallback occurred (e.g., "Preferred model ... failed", "Attempting fallback with ...").
10. **Validation:** Verify `LastRead` update and notification call.

### Scenario 6: Chatroom Without Persona

**Objective:** Verify that sending a message in a chatroom without an assigned persona does not
trigger AI processing. **Pre-conditions:**

1. Seed `testUser` and another regular user (`otherUser`).
2. Seed a `chatroom` containing both users, ensuring `PersonaUserId` is `null`.
3. Seed `LastRead` for both users. **Test Steps & Validation:**
4. Create `newUserMessageDto` from `testUser`.
5. Call `POST /api/chatrooms/{chatroomId}/chats`. Assert 201.
6. Wait for a reasonable duration (e.g., 5 seconds).
7. Query `ChatMessages` table for any messages from a potential persona user ID _or_ any messages
   created significantly after the `newUserMessageDto`.
8. **Validation:** Assert that _no_ unexpected messages were added. Only the `newUserMessageDto`
   should exist after the initial seeding.
9. **Validation:** Verify `mockNotificationService` was called only _once_ (for the original user
   message), not a second time for a persona response.
