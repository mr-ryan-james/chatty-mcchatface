using System.Threading.Tasks;
using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChattyMcChatface.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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
            Token = token,
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
}