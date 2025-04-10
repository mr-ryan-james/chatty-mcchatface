using ChattyMcChatface.Core.Dtos;
using ChattyMcChatface.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace ChattyMcChatface.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonasController : ControllerBase
    {
        private readonly IPersonaConfigService _personaConfigService;

        public PersonasController(IPersonaConfigService personaConfigService)
        {
            _personaConfigService = personaConfigService;
        }

        /// <summary>
        /// Gets all available persona configurations.
        /// </summary>
        /// <returns>A list of persona configurations.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PersonaConfig>), 200)]
        public IActionResult GetPersonas()
        {
            var personas = _personaConfigService.GetAllConfigs(); // Use GetAllConfigs as identified earlier
            return Ok(personas);
        }
    }
}