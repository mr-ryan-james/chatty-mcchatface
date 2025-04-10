# AI Persona Integration Plan - Backend

## 1. Add Third Persona to `personas.json`

**File:** `dotnet_server/ChattyMcChatface.Api/personas.json`

Add a new persona object, e.g.:

```json
{
    "id": "persona3",
    "name": "Ava",
    "description": "A witty, empathetic AI assistant.",
    "model": "gpt-4",
    "prompt": "You are Ava, a witty and empathetic assistant who helps users with their questions."
}
```

---

## 2. Create `PersonasController` with `GET /api/personas`

**File:** `dotnet_server/ChattyMcChatface.Api/Controllers/PersonasController.cs`

-   Inject `IPersonaConfigService`.
-   Implement an endpoint to return all persona configs.

```csharp
[ApiController]
[Route("api/[controller]")]
public class PersonasController : ControllerBase
{
    private readonly IPersonaConfigService _personaConfigService;

    public PersonasController(IPersonaConfigService personaConfigService)
    {
        _personaConfigService = personaConfigService;
    }

    [HttpGet]
    public IActionResult GetPersonas()
    {
        var personas = _personaConfigService.GetAllPersonas();
        return Ok(personas);
    }
}
```

**DI Registration:** Ensure `IPersonaConfigService` is registered in `Program.cs` or `Startup.cs`.

---

## 3. Modify `CreateChatroomDto` to Include `PersonaUserId`

**File:** `dotnet_server/ChattyMcChatface.Core/Dtos/CreateChatroomDto.cs`

Add:

```csharp
public Guid? PersonaUserId { get; set; }
```

---

## 4. Update `ChatroomsController.cs` - `CreateChatroom`

**File:** `dotnet_server/ChattyMcChatface.Api/Controllers/ChatroomsController.cs`

-   Accept `PersonaUserId` from DTO.
-   Validate persona user exists.
-   Set `chatroom.PersonaUserId`.
-   Add persona user to chatroom members.

**Snippet:**

```csharp
if (dto.PersonaUserId.HasValue)
{
    var personaUser = await _context.Users.FindAsync(dto.PersonaUserId.Value);
    if (personaUser == null || !personaUser.IsPersona)
        return BadRequest("Invalid persona user ID.");

    chatroom.PersonaUserId = dto.PersonaUserId;
    chatroom.Members.Add(new ChatroomMember { UserId = dto.PersonaUserId.Value });
}
```

---

## 5. Update `ChatroomsController.cs` - `AddChatMessage`

-   Inject `IPersonaService`.
-   After saving user message, if chatroom has a persona, trigger persona response generation.
-   Consider background execution (e.g., `Task.Run` or background queue).

**Snippet:**

```csharp
if (chatroom.PersonaUserId.HasValue)
{
    _ = Task.Run(() => _personaService.GenerateResponseAsync(chatroom.Id, message.Id));
}
```

---

## 6. Implement Persona Read Status Tracking

**File:** `dotnet_server/ChattyMcChatface.Core/Services/PersonaService.cs`

In `GenerateResponseAsync`:

-   After saving persona message, update `LastRead` entity for persona user in that chatroom.

**Snippet:**

```csharp
var lastRead = await _context.LastReads
    .FirstOrDefaultAsync(lr => lr.ChatroomId == chatroomId && lr.UserId == personaUserId);

if (lastRead == null)
{
    lastRead = new LastRead { ChatroomId = chatroomId, UserId = personaUserId, LastReadMessageId = personaMessage.Id };
    _context.LastReads.Add(lastRead);
}
else
{
    lastRead.LastReadMessageId = personaMessage.Id;
}

await _context.SaveChangesAsync();
```

---

## 7. Enhance `ChatroomDetailDto` and `GetChatroom`

**File:** `dotnet_server/ChattyMcChatface.Core/Dtos/ChatroomDetailDto.cs`

Add:

```csharp
public PersonaConfig PersonaConfig { get; set; }
```

**File:** `dotnet_server/ChattyMcChatface.Api/Controllers/ChatroomsController.cs`

In `GetChatroom`:

-   If `chatroom.PersonaUserId` exists, fetch persona config via `IPersonaConfigService`.
-   Populate `ChatroomDetailDto.PersonaConfig`.

---

## 8. Cleanup Unused Endpoints

Consider removing:

-   `GET /chatrooms/{id}/persona`
-   `GET /chatrooms/{id}/messagesWithPersona`

**Files:** `ChatroomsController.cs` and related service methods.

---

## 9. Entity Relationship Diagram (Optional)

```mermaid
erDiagram
    Chatroom {
        Guid Id
        Guid? PersonaUserId
    }
    User {
        Guid Id
        bool IsPersona
    }
    Chatroom ||--o{ User : Members
    Chatroom ||--o{ ChatMessage : Messages
    User ||--o{ ChatMessage : Sends
    User ||--o{ LastRead : Reads
    Chatroom ||--o{ LastRead : Tracks
```

---

## Summary

This plan details the backend changes to support AI personas, including persona management, chatroom
association, message handling, and persona response generation.
