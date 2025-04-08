using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ChattyMcChatface.Api.Controllers;

public abstract class BaseApiController : ControllerBase
{
    protected int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims or is invalid");
        }
        
        return userId;
    }
}