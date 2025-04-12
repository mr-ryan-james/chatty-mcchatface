using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChattyMcChatface.Data; // For AppDbContext
using Microsoft.AspNetCore.Authorization; // For [Authorize]

namespace ChattyMcChatface.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _context;

    public AuthController(IAuthService authService, AppDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegisterDto registerDto)
    {
        var (user, token) = await _authService.RegisterAsync(registerDto);
        
        if (user == null)
        {
            return BadRequest("Registration failed. Email may already be in use.");
        }
        
        return Ok(new AuthResponseDto
        {
            Token = token!, // Assumed non-null after successful registration check
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            UserId = user.Id
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginDto loginDto)
    {
        var response = await _authService.LoginAsync(loginDto);
        
        if (response == null)
        {
            return Unauthorized("Invalid email or password");
        }
        
        return Ok(response);
    }
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        int currentUserId = GetCurrentUserId(); // From BaseApiController

        var user = await _context.Users.FindAsync(currentUserId);

        if (user == null)
        {
            // This shouldn't happen if the user is authorized, but handle defensively
            return NotFound("User not found.");
        }

        // Map to UserDto (ensure UserDto includes necessary fields like id, firstName, lastName, email)
        var userDto = new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            CreatedAt = user.CreatedAt // Include if needed by frontend User model
            // Add other fields if your UserDto and frontend User model require them
        };

        return Ok(userDto);
    }
}