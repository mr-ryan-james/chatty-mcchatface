using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChattyMcChatface.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : BaseApiController
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        int currentUserId = GetCurrentUserId();

        var users = await _context.Users
            .Where(u => u.Id != currentUserId) // Exclude self
            .Where(u => !u.IsPersona) // Exclude personas
            .Select(u => new UserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
        
        return Ok(users);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        
        if (user == null)
        {
            return NotFound();
        }
        
        var userDto = new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
        
        return Ok(userDto);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        
        if (user == null)
        {
            return NotFound();
        }
        
        // Optional: Add check for admin or self-delete
        int currentUserId = GetCurrentUserId();
        if (id != currentUserId)
        {
            return Forbid();
        }
        
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}