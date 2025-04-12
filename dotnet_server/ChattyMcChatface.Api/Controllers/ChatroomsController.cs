using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services;
using ChattyMcChatface.Data;
using ChattyMcChatface.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ChattyMcChatface.Api.Hubs;

namespace ChattyMcChatface.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatroomsController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;

    private readonly IPersonaService _personaService;

    public ChatroomsController(
        AppDbContext context,
        IHubContext<ChatHub> hubContext,
        IPersonaService personaService)
    {
        _context = context;
        _hubContext = hubContext;
        _personaService = personaService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<ChatroomDto>>> GetChatrooms()
    {
        int currentUserId = GetCurrentUserId();
        
        var chatrooms = await _context.Chatrooms
            .Include(c => c.Users)
            .Include(c => c.Chats)
            .Where(c => c.Users.Any(u => u.Id == currentUserId))
            .Select(c => new ChatroomDto
            {
                Id = c.Id,
                Title = c.Title,
                Date = c.Date,
                Users = c.Users.Select(u => new UserDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt
                }).ToList(),
                ChatCount = c.Chats.Count
            })
            .ToListAsync();
        
        return Ok(chatrooms);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ChatroomDto>> CreateChatroom([FromBody] CreateChatroomDto createChatroomDto)
    {
        int currentUserId = GetCurrentUserId();
        
        // Get current user
        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (currentUser == null)
        {
            return Unauthorized();
        }
        
        // Get all users for the chatroom
        var users = await _context.Users
            .Where(u => createChatroomDto.UserIds.Contains(u.Id) || u.Id == currentUserId)
            .ToListAsync();

        // Validate and add PersonaUser if provided
        if (createChatroomDto.PersonaUserId.HasValue)
        {
            var personaUser = await _context.Users.FindAsync(createChatroomDto.PersonaUserId.Value);
            if (personaUser == null || !personaUser.IsPersona)
            {
                return BadRequest("Invalid persona user id");
            }

            // Add persona user to chatroom users if not already included
            if (!users.Any(u => u.Id == createChatroomDto.PersonaUserId.Value))
            {
                users.Add(personaUser);
            }
        }
        
        // Create new chatroom
        var chatroom = new Chatroom
        {
            Title = createChatroomDto.Title,
            Users = users,
            PersonaUserId = createChatroomDto.PersonaUserId // Directly use the int? value
        };
        
        _context.Chatrooms.Add(chatroom);
        await _context.SaveChangesAsync();
        
        // Map to DTO for response
        var chatroomDto = new ChatroomDto
        {
            Id = chatroom.Id,
            Title = chatroom.Title,
            Date = chatroom.Date,
            Users = chatroom.Users.Select(u => new UserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                CreatedAt = u.CreatedAt
            }).ToList(),
            ChatCount = 0
        };
        
        return CreatedAtAction(nameof(GetChatroom), new { id = chatroom.Id }, chatroomDto);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ChatroomDetailDto>> GetChatroom(int id)
    {
        int currentUserId = GetCurrentUserId();
        
        var chatroom = await _context.Chatrooms
            .Include(c => c.Users)
            .Include(c => c.Chats)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == id);
        
        if (chatroom == null)
        {
            return NotFound();
        }
        
        // Check if user has access to this chatroom
        if (!chatroom.Users.Any(u => u.Id == currentUserId))
        {
            return Forbid();
        }
        
        var chatroomDetailDto = new ChatroomDetailDto
        {
            Id = chatroom.Id,
            Title = chatroom.Title,
            Date = chatroom.Date,
            Users = chatroom.Users.Select(u => new UserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                CreatedAt = u.CreatedAt
            }).ToList(),
            Messages = chatroom.Chats
                .OrderBy(m => m.Date)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    Text = m.Text,
                    Date = m.Date,
                    UserId = m.UserId,
                    UserFirstName = m.User.FirstName,
                    UserLastName = m.User.LastName,
                    ChatroomId = m.ChatroomId,
                    Role = m.Role
                }).ToList()
        };

        if (chatroom.PersonaUserId.HasValue)
        {
            var personaUser = await _context.Users.FindAsync(chatroom.PersonaUserId.Value);
            if (personaUser != null && personaUser.IsPersona)
            {
                chatroomDetailDto.PersonaConfig = new PersonaConfig
                {
                    PersonaUserId = personaUser.Id,
                    DisplayName = personaUser.FirstName ?? "",
#pragma warning disable CS8601 // Source and destination are both nullable strings
                    SystemPrompt = personaUser.SystemPrompt,
                    PreferredModelId = personaUser.PreferredModelId
#pragma warning restore CS8601
                };
            }
        }
        
        return Ok(chatroomDetailDto);
    }

    [HttpPost("{id}/chats")]
    [Authorize]
    public async Task<ActionResult<ChatMessageDto>> AddChatMessage(ChatMessageDto chatMessageDto)
    {
        int currentUserId = GetCurrentUserId();

        var chatroom = await _context.Chatrooms
            .Include(c => c.Users)
            .FirstOrDefaultAsync(c => c.Id == chatMessageDto.ChatroomId);

        if (chatroom == null)
        {
            return NotFound();
        }

        if (!chatroom.Users.Any(u => u.Id == currentUserId))
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(currentUserId);

        if (user == null)
        {
            return Unauthorized();
        }

        var chatMessage = new ChatMessage
        {
            Text = chatMessageDto.Text,
            UserId = currentUserId,
            User = user,
            ChatroomId = chatroom.Id,
            Chatroom = chatroom
        };

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        var savedChatMessageDto = new ChatMessageDto
        {
            Id = chatMessage.Id,
            Text = chatMessage.Text,
            Date = chatMessage.Date,
            UserId = chatMessage.UserId,
            UserFirstName = user.FirstName,
            UserLastName = user.LastName,
            ChatroomId = chatMessage.ChatroomId
        };

        await _hubContext.Clients.Group(chatroom.Id.ToString()).SendAsync("ReceiveMessage", chatroom.Id, savedChatMessageDto);

        // If persona is assigned, generate persona response
        if (chatroom.PersonaUserId.HasValue)
        {
            await _personaService.GenerateResponseAsync(chatroom.Id, savedChatMessageDto);
        }

        return CreatedAtAction(nameof(GetChatroom), new { id = chatroom.Id }, savedChatMessageDto);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<ChatroomDto>> UpdateChatroom(int id, UpdateChatroomDto updateChatroomDto)
    {
        int currentUserId = GetCurrentUserId();
        
        var chatroom = await _context.Chatrooms
            .Include(c => c.Users)
            .Include(c => c.Chats)
            .FirstOrDefaultAsync(c => c.Id == id);
        
        if (chatroom == null)
        {
            return NotFound();
        }
        
        // Check if user has access
        if (!chatroom.Users.Any(u => u.Id == currentUserId))
        {
            return Forbid();
        }
        
        // Update title
        chatroom.Title = updateChatroomDto.Title;
        
        // Add users if specified
        if (updateChatroomDto.AddUserIds != null && updateChatroomDto.AddUserIds.Count > 0)
        {
            var usersToAdd = await _context.Users
                .Where(u => updateChatroomDto.AddUserIds.Contains(u.Id))
                .ToListAsync();
                
            foreach (var user in usersToAdd)
            {
                if (!chatroom.Users.Any(u => u.Id == user.Id))
                {
                    chatroom.Users.Add(user);
                }
            }
        }
        
        // Remove users if specified
        if (updateChatroomDto.RemoveUserIds != null && updateChatroomDto.RemoveUserIds.Count > 0)
        {
            var usersToRemove = chatroom.Users
                .Where(u => updateChatroomDto.RemoveUserIds.Contains(u.Id))
                .ToList();
                
            foreach (var user in usersToRemove)
            {
                chatroom.Users.Remove(user);
            }
        }
        
        _context.Entry(chatroom).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        
        // Map to DTO for response
        var chatroomDto = new ChatroomDto
        {
            Id = chatroom.Id,
            Title = chatroom.Title,
            Date = chatroom.Date,
            Users = chatroom.Users.Select(u => new UserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                CreatedAt = u.CreatedAt
            }).ToList(),
            ChatCount = chatroom.Chats.Count
        };
        
        return Ok(chatroomDto);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteChatroom(int id)
    {
        int currentUserId = GetCurrentUserId();
        
        var chatroom = await _context.Chatrooms
            .Include(c => c.Users)
            .FirstOrDefaultAsync(c => c.Id == id);
        
        if (chatroom == null)
        {
            return NotFound();
        }
        
        // Check if user has access
        if (!chatroom.Users.Any(u => u.Id == currentUserId))
        {
            return Forbid();
        }
        
        // Optional: Add check if user is creator (not implemented - would need a CreatorId field)
        
        _context.Chatrooms.Remove(chatroom);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}