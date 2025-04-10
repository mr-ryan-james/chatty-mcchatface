# AI Persona Integration Plan - Frontend

## 1. Fetch Personas from Backend

**File:** `angular-client/src/app/shared/services/chat.service.ts`

-   Define `PersonaConfig` interface:

```typescript
export interface PersonaConfig {
    id: string
    name: string
    description: string
    model: string
    prompt: string
}
```

-   Add method to fetch personas:

```typescript
getPersonas(): Observable<PersonaConfig[]> {
  return this.http.get<PersonaConfig[]>(`${this.apiUrl}/personas`);
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
-   Add a dropdown/select in the template:

```html
<label for="persona">Persona:</label>
<select [(ngModel)]="selectedPersonaUserId">
    <option [ngValue]="null">No Persona</option>
    <option
        *ngFor="let persona of personas"
        [ngValue]="persona.id"
    >
        {{ persona.name }}
    </option>
</select>
```

-   In submit logic, include `personaUserId` in the payload:

```typescript
const chatroomDto: CreateChatroomDto = {
    title: this.title,
    personaUserId: this.selectedPersonaUserId,
    // other fields...
}
```

---

## 3. Align DTO Interfaces

**File:** `angular-client/src/app/shared/services/chat.service.ts`

Update or add:

```typescript
export interface CreateChatroomDto {
    title: string
    personaUserId?: string | null
    // other fields...
}

export interface ChatroomDetailDto {
    id: string
    title: string
    personaUserId?: string | null
    personaConfig?: PersonaConfig | null
    // other fields...
}

export interface ChatroomDto {
    id: string
    title: string
    personaUserId?: string | null
    // other fields...
}
```

---

## 4. Display Persona Info in Chat Room

**Directory:** `angular-client/src/app/chat/chat-room/`

-   **Component:** `chat-room.component.ts`
-   **Template:** `chat-room.component.html`

### Changes:

-   When loading chatroom details, display persona info if present:

```html
<div *ngIf="chatroom.personaConfig">
    <h3>Persona: {{ chatroom.personaConfig.name }}</h3>
    <p>{{ chatroom.personaConfig.description }}</p>
</div>
```

---

## 5. Differentiate Persona Messages

-   In chat message list, visually distinguish persona messages.

Example (template):

```html
<div
    *ngFor="let message of messages"
    [ngClass]="{'persona-message': message.userId === chatroom.personaUserId}"
>
    <strong *ngIf="message.userId === chatroom.personaUserId"
        >{{ chatroom.personaConfig?.name }}:</strong
    >
    <strong *ngIf="message.userId !== chatroom.personaUserId">{{ message.userName }}:</strong>
    {{ message.content }}
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

Remove:

-   `getChatroomPersona()`
-   `getChatroomMessagesWithPersona()`

---

## Summary

-   Fetch personas and display in chat creation.
-   Send selected persona ID when creating chatroom.
-   Show persona info in chatroom view.
-   Visually differentiate persona messages.
-   Align DTOs/interfaces with backend.
-   Remove obsolete persona-related methods.

This plan ensures a smooth frontend integration of AI personas.
