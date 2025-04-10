# AI Persona Integration Plan - Frontend

## 1. Fetch Personas from Backend

**File:** `angular-client/src/app/shared/services/chat.service.ts`

-   Define `PersonaConfig` interface (Matches backend DTO):

```typescript
export interface PersonaConfig {
    personaUserId: number // Matches backend User ID (int)
    displayName: string
    systemPrompt: string
    preferredModelId: string
}
```

-   Add method to fetch personas:

```typescript
getPersonas(): Observable<PersonaConfig[]> {
  // Ensure correct API URL is used from environment
  return this.http.get<PersonaConfig[]>(`${environment.apiUrl}/personas`, this.getAuthHeaders());
}
```

---

## 2. Update Chat Creation Component to Select Persona

**Directory:** `angular-client/src/app/chat/chat-create/`

-   **Component:** `chat-create.component.ts`
-   **Template:** `chat-create.component.html`

### Changes:

-   On init, call `chatService.getPersonas()` to fetch persona list.
-   Store personas in a property, e.g., `personas: PersonaConfig[] = []`.
-   Add a property for the selected ID, e.g., `selectedPersonaUserId: number | null = null;`.
-   Add a dropdown/select in the template:

```html
<label for="persona">Persona:</label>
<select [(ngModel)]="selectedPersonaUserId">
    <option [ngValue]="null">No Persona</option>
    <option
        *ngFor="let persona of personas"
        [ngValue]="persona.personaUserId"
    >
        {{ persona.displayName }}
    </option>
</select>
```

-   In submit logic, include `personaUserId` (as string or null) and `userIds` (as numbers) in the
    payload:

```typescript
// Assuming selectedUserIds is number[] and title is string
const chatroomDto: CreateChatroomDto = {
    title: this.title,
    personaUserId: this.selectedPersonaUserId ? this.selectedPersonaUserId.toString() : null,
    userIds: this.selectedUserIds,
}
// Call chatService.createChatroom(chatroomDto)...
```

---

## 3. Align DTO Interfaces

**File:** `angular-client/src/app/shared/services/chat.service.ts`

Update or add (Reflecting backend structure and planned fixes):

```typescript
// Matches backend dotnet_server/ChattyMcChatface.Core/Dtos/CreateChatroomDto.cs
export interface CreateChatroomDto {
    title: string
    personaUserId?: string | null // Backend uses string?
    userIds: number[] // Backend uses List<int>
}

// Matches backend dotnet_server/ChattyMcChatface.Core/Dtos/ChatroomDto.cs
// Used for lists - does NOT contain personaUserId from backend
export interface ChatroomDto {
    id: number // Backend uses int
    title: string
    date: Date // Backend uses DateTime
    users: UserDto[] // Backend uses List<UserDto>
    chatCount: number // Backend uses int
}

// Matches backend dotnet_server/ChattyMcChatface.Core/Dtos/ChatroomDetailDto.cs
// Extends ChatroomDto for consistency (though backend doesn't use inheritance here)
export interface ChatroomDetailDto extends ChatroomDto {
    // Properties from ChatroomDto are inherited implicitly if extending
    // id: number;
    // title: string;
    // date: Date;
    // users: UserDto[];
    // chatCount: number; // Note: Backend ChatroomDetailDto doesn't explicitly include ChatCount

    personaUserId?: string | null // Backend uses string?
    personaConfig?: PersonaConfig | null // Backend uses PersonaConfig?
    messages: ChatMessageDto[] // Backend uses List<ChatMessageDto> Messages
}

// Matches backend dotnet_server/ChattyMcChatface.Core/Dtos/ChatMessageDto.cs
export interface ChatMessageDto {
    id: number // Backend uses int
    text: string
    date: Date // Backend uses DateTime
    userId: number // Backend uses int
    userFirstName?: string // Backend includes this
    userLastName?: string // Backend includes this
    chatroomId: number // Backend uses int
    role?: MessageRole // Optional: For UI logic, not directly from backend DTO
}

// Matches backend dotnet_server/ChattyMcChatface.Core/Dtos/UserDto.cs
export interface UserDto {
    id: number // Backend uses int
    firstName?: string
    lastName?: string
    email?: string
    createdAt: Date // Backend uses DateTime
}

// Enum for message role (UI helper)
export enum MessageRole {
    User = "user",
    Assistant = "assistant",
}
```

---

## 4. Display Persona Info in Chat Room

**Directory:** `angular-client/src/app/chat/chat-room/`

-   **Component:** `chat-room.component.ts` (Should fetch `ChatroomDetailDto`)
-   **Template:** `chat-room.component.html`

### Changes:

-   When loading chatroom details (`ChatroomDetailDto`), display persona info if present:

```html
<!-- Assuming 'chatroom' property holds the ChatroomDetailDto -->
<div *ngIf="chatroom.personaConfig">
    <h3>Persona: {{ chatroom.personaConfig.displayName }}</h3>
    <!-- SystemPrompt could be displayed if needed: <p>{{ chatroom.personaConfig.systemPrompt }}</p> -->
</div>
```

---

## 5. Differentiate Persona Messages

**Directory:** `angular-client/src/app/chat/chat-room/`

-   **Component:** `chat-room.component.ts`
-   **Template:** `chat-room.component.html`

### Changes:

-   In chat message list (`chatroom.messages`), visually distinguish persona messages.

Example (template):

```html
<!-- Assuming 'chatroom' holds ChatroomDetailDto and 'messages' holds ChatMessageDto[] -->
<div
    *ngFor="let message of messages"
    [ngClass]="{'persona-message': message.userId === chatroom.personaUserId}"
>
    <!-- Display Persona Name if message is from persona -->
    <strong *ngIf="message.userId === chatroom.personaUserId"
        >{{ chatroom.personaConfig?.displayName }}:</strong
    >
    <!-- Display User Name if message is not from persona -->
    <strong *ngIf="message.userId !== chatroom.personaUserId"
        >{{ message.userFirstName }} {{ message.userLastName }}:</strong
    >
    {{ message.text }}
    <!-- Use message.text -->
</div>
```

-   Style `.persona-message` in CSS:

```css
.persona-message {
    background-color: #eef;
    font-style: italic;
}
```

---

## 6. Clean Up Unused Methods

**File:** `angular-client/src/app/shared/services/chat.service.ts`

Remove (If confirmed unused after other changes):

-   `getChatroomPersona()`
-   `getChatroomMessagesWithPersona()`

---

## Summary

-   Fetch personas and display in chat creation.
-   Send selected persona ID when creating chatroom.
-   **Align DTOs/interfaces with backend (IDs as numbers, UserIds as numbers, use `messages`).**
-   Show persona info (`displayName`) in chatroom view using `ChatroomDetailDto`.
-   Visually differentiate persona messages using `personaUserId` and display `displayName`.
-   Remove obsolete persona-related methods if applicable.

This plan ensures a smooth frontend integration of AI personas, aligned with the backend
implementation.
