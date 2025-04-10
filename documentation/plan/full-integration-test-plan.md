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
