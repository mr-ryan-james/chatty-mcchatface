using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Data.Entities;

namespace ChattyMcChatface.Core.Services;
public interface IAuthService
{
    Task<User?> RegisterAsync(UserRegisterDto registerDto);
    Task<AuthResponseDto?> LoginAsync(UserLoginDto loginDto);
}