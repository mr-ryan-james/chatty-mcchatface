using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ChattyMcChatface.Data;

// Define a simple record for the response
public record PersonaInfo(int Id, string DisplayName, string? SystemPrompt, string? PreferredModelId);

namespace ChattyMcChatface.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PersonasController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all available persona configurations.
        /// </summary>
        /// <returns>A list of persona configurations.</returns>
        [HttpGet]
        public async Task<ActionResult<List<PersonaInfo>>> GetPersonas()
        {
            var personas = await _context.Users
                .Where(u => u.IsPersona)
                .Select(u => new PersonaInfo(
                    u.Id,
                    u.FirstName ?? $"Persona {u.Id}", // Removed u.Username fallback
                    u.SystemPrompt,
                    u.PreferredModelId
                ))
                .ToListAsync();

            return Ok(personas);
        }
    }
}