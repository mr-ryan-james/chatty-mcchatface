using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ChattyMcChatface.Data;
using ChattyMcChatface.Core.Services;
using ChattyMcChatface.Core.Services.AI;
using ChattyMcChatface.Api.Hubs;
using ChattyMcChatface.Api.Services;
using ChattyMcChatface.Core.Services.AI.OpenAI;
using ChattyMcChatface.Core.Services.AI.Azure;
using ChattyMcChatface.Core.Services.AI.Claude;
using ChattyMcChatface.Core.Services.AI.Gemini;
using ChattyMcChatface.Core.Services.AI.Vertex;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Docker.json", optional: true, reloadOnChange: false);

// Add services to the container.
// Add controller services
builder.Services.AddControllers();

// Add Swagger/OpenAPI support
builder.Services.AddEndpointsApiExplorer();

// Add SignalR services
builder.Services.AddSignalR();

// Use a single connection for the in-memory database to persist data across requests
var connectionString = "DataSource=ChattyDb;mode=memory;cache=shared";
var keepAliveConnection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
keepAliveConnection.Open(); // Keep the connection open for the lifetime of the application

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(keepAliveConnection); // Use the shared connection
});

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IPersonaConfigService, PersonaConfigService>();

// Register HttpClient for REST API clients
builder.Services.AddHttpClient(); // General HTTP client factory registration
builder.Services.AddHttpClient("GeminiApi", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register all AI providers
builder.Services.AddScoped<OpenAiProvider>();
builder.Services.AddScoped<AzureAiProvider>();
builder.Services.AddScoped<GeminiProvider>();
builder.Services.AddScoped<ClaudeProvider>();
builder.Services.AddScoped<VertexAiProvider>();

// Register model classes for AI providers
builder.Services.AddSingleton<OpenAiModels>();
builder.Services.AddSingleton<AzureAiModels>();
builder.Services.AddSingleton<ClaudeModels>();
builder.Services.AddSingleton<GeminiModels>();
builder.Services.AddSingleton<VertexAiModels>();

// Register notification service
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

// Register persona service
builder.Services.AddScoped<IPersonaService, PersonaService>();

// Configure Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)) // Ensure Key is not null
    };
    
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // If the request is for our hub...
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/chathub"))) // Check if path starts with /chathub
            {
                // Read the token from the query string
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Configure Authorization
builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Define CORS policy
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins"; // Define policy name

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:4200") // Angular dev server default
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials(); // Important for SignalR authentication with cookies/tokens
                    });
});

var app = builder.Build();

// Ensure database is created (for in-memory) and seed persona users
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
        
        // Seed persona users in development environment
        if (app.Environment.IsDevelopment())
        {
            SeedPersonaUsers(services);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB or seeding data.");
    }
}

/// <summary>
/// Seeds user accounts for each persona defined in the configuration
/// </summary>
/// <param name="serviceProvider">Application service provider</param>
static void SeedPersonaUsers(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var scopedServices = scope.ServiceProvider;
    var dbContext = scopedServices.GetRequiredService<AppDbContext>();
    var personaConfigService = scopedServices.GetRequiredService<IPersonaConfigService>();
    var logger = scopedServices.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Seeding persona user accounts...");
        
        // Get all persona configurations
        var personaConfigs = personaConfigService.GetAllConfigs();
        
        foreach (var persona in personaConfigs)
        {
            // Check if this persona user already exists
            var existingUser = dbContext.Users.FirstOrDefault(u => u.Id == persona.PersonaUserId);
            
            if (existingUser == null)
            {
                // Create new user entity for the persona
                var personaUser = new ChattyMcChatface.Data.Entities.User
                {
                    Id = persona.PersonaUserId,
                    FirstName = persona.DisplayName,
                    LastName = "AI",
                    Email = $"{persona.DisplayName.ToLower().Replace(" ", "")}@chatty.ai",
                    PasswordHash = "PERSONA_NO_LOGIN", // Personas don't login with passwords
                    IsPersona = true
                };
                
                // Add the new persona user
                dbContext.Users.Add(personaUser);
                logger.LogInformation($"Added persona user: {persona.DisplayName} (ID: {persona.PersonaUserId})");
            }
        }
        
        // Save changes to the database
        dbContext.SaveChanges();
        logger.LogInformation("Persona users seeding completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding persona users.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Development-specific middleware can be added here
}
app.UseHttpsRedirection();

app.UseRouting(); // 1. Routing

app.UseCors(MyAllowSpecificOrigins); // 2. CORS
app.UseAuthentication();           // 3. Authentication
app.UseAuthorization();          // 4. Authorization

app.MapControllers();            // 5. Map API Controllers
app.MapHub<ChatHub>("/chathub"); // 6. Map SignalR Hub

app.UseDefaultFiles();           // 7. Default Files (index.html)
app.UseStaticFiles();            // 8. Static Files (js, css, images)

app.MapFallbackToFile("index.html"); // 9. SPA Fallback

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
