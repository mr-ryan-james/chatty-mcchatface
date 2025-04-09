using ChattyMcChatface.Core.Dtos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ChattyMcChatface.Core.Services;

public class PersonaConfigService : IPersonaConfigService
{
    private readonly ILogger<PersonaConfigService> _logger;
    private readonly Dictionary<int, PersonaConfig> _personaConfigs;

    public PersonaConfigService(ILogger<PersonaConfigService> logger)
    {
        _logger = logger;
        _personaConfigs = new Dictionary<int, PersonaConfig>();
        
        try
        {
            // Get the application's base directory
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(baseDirectory, "personas.json");
            
            _logger.LogInformation("Loading persona configurations from {FilePath}", filePath);
            
            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                var personaList = JsonSerializer.Deserialize<List<PersonaConfig>>(jsonContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (personaList != null)
                {
                    foreach (var persona in personaList)
                    {
                        _personaConfigs[persona.PersonaUserId] = persona;
                    }
                    _logger.LogInformation("Successfully loaded {Count} persona configurations", _personaConfigs.Count);
                }
                else
                {
                    _logger.LogWarning("Personas.json file was empty or not properly formatted");
                }
            }
            else
            {
                _logger.LogWarning("Personas.json file not found at {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading persona configurations");
        }
    }

    /// <summary>
    /// Gets a persona configuration by its user ID
    /// </summary>
    /// <param name="personaUserId">The ID of the persona user</param>
    /// <returns>The persona configuration or null if not found</returns>
    public PersonaConfig? GetConfig(int personaUserId)
    {
        if (_personaConfigs.TryGetValue(personaUserId, out var config))
        {
            return config;
        }
        
        _logger.LogWarning("Persona configuration not found for ID: {PersonaUserId}", personaUserId);
        return null;
    }
    
    /// <summary>
    /// Gets all available persona configurations
    /// </summary>
    /// <returns>Collection of all persona configurations</returns>
    public IEnumerable<PersonaConfig> GetAllConfigs()
    {
        return _personaConfigs.Values;
    }
}