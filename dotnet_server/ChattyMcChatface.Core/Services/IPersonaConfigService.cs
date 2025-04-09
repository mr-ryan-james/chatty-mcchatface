using ChattyMcChatface.Core.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChattyMcChatface.Core.Services;

public interface IPersonaConfigService
{
    /// <summary>
    /// Gets a persona configuration by its user ID
    /// </summary>
    /// <param name="personaUserId">The ID of the persona user</param>
    /// <returns>The persona configuration or null if not found</returns>
    PersonaConfig? GetConfig(int personaUserId);
    
    /// <summary>
    /// Gets all available persona configurations
    /// </summary>
    /// <returns>Collection of all persona configurations</returns>
    IEnumerable<PersonaConfig> GetAllConfigs();
}