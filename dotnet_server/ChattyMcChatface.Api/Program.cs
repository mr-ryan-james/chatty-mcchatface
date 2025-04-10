using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ChattyMcChatface.Data; // Assuming this is where AppDbContext is
using Microsoft.EntityFrameworkCore; // For UseSqlite, UseNpgsql etc.
using Microsoft.AspNetCore.Authentication.JwtBearer; // For JWT authentication
using Microsoft.IdentityModel.Tokens; // For TokenValidationParameters
using System.Net.Http; // Add this line
using System.Text; // For Encoding
using Microsoft.OpenApi.Models;

using ChattyMcChatface.Core.Services;
using ChattyMcChatface.Api.Hubs;
using ChattyMcChatface.Api.Services;
using ChattyMcChatface.Core.Services.AI;
namespace ChattyMcChatface.Api
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add DbContext configuration
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString ?? "DataSource=chatty.db")); // Provide a default connection string if needed

            // Add Authentication services
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")))
                    };
                });

            // Add other services (CORS, etc.)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder => builder.AllowAnyOrigin()
                                    .AllowAnyMethod()
                                    .AllowAnyHeader());
            });


            // Add SignalR
            builder.Services.AddSignalR();

            // Register application services (adjust lifetimes as needed - Scoped is common for services using DbContext)
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IPersonaService, PersonaService>();
            builder.Services.AddSingleton<IPersonaConfigService, PersonaConfigService>(); // Singleton as it reads from a file
            builder.Services.AddScoped<INotificationService, SignalRNotificationService>(); // Assuming SignalRNotificationService exists
            builder.Services.AddHttpClient(); // Add this line

                        // Register AI Model Providers/Holders (adjust lifetimes as needed)
            
                        // Register AI Providers (Scoped lifetime is often suitable)
                        builder.Services.AddScoped<OpenAiProvider>();
                        builder.Services.AddScoped<AzureAiProvider>(sp =>
                        {
                            var config = sp.GetRequiredService<IConfiguration>();
                            var logger = sp.GetRequiredService<ILogger<AzureAiProvider>>();
                            
                            // Assuming configuration keys like "AzureOpenAI:ApiKey" and "AzureOpenAI:Endpoint"
                            // Adjust these keys if they are different (e.g., nested under specific deployment names like "Thrivify")
                            // For simplicity, let's assume top-level keys for now. If this fails, we might need to adjust based on actual config structure.
                            string? apiKey = config["AzureOpenAI:ApiKey"];
                            string? endpoint = config["AzureOpenAI:Endpoint"];
                        
                            if (string.IsNullOrEmpty(apiKey))
                            {
                                throw new InvalidOperationException("Azure OpenAI API key ('AzureOpenAI:ApiKey') is not configured.");
                            }
                            if (string.IsNullOrEmpty(endpoint))
                            {
                                throw new InvalidOperationException("Azure OpenAI endpoint ('AzureOpenAI:Endpoint') is not configured.");
                            }
                        
                            return new AzureAiProvider(apiKey, endpoint, logger);
                        });
                        builder.Services.AddScoped<ClaudeProvider>();
                        builder.Services.AddScoped<GeminiProvider>();
                        builder.Services.AddScoped<VertexAiProvider>();
            
                        // Optional: Register HttpClient for providers that need it (if not already handled internally)
                        // builder.Services.AddHttpClient<OpenAiProvider>(); // Example
                        // builder.Services.AddHttpClient<AzureAiProvider>(); // Example
                        // ... add others if needed ...
            // Register individual AI providers if needed, e.g.:
            // builder.Services.AddHttpClient<OpenAiProvider>(); // If using HttpClientFactory
            // builder.Services.AddScoped<IAiProvider, OpenAiProvider>(); // Example if using a common interface

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                // Development specific configurations can be added here
            }
            else
            {
                // Production specific configurations
                app.UseExceptionHandler("/Error");
                app.UseHsts(); // Enforce HTTPS in production
            }

            app.UseHttpsRedirection(); // Enforce HTTPS

            app.UseDefaultFiles(); // Serve index.html for root path requests
            app.UseStaticFiles(); // Serve files from wwwroot

            app.UseCors("AllowAll"); // Apply CORS policy

            app.UseAuthentication(); // Enable authentication middleware
            app.UseAuthorization();

            app.MapControllers();

            app.MapHub<ChatHub>("/chathub"); // Map the ChatHub
            app.Run();
        }
    }
}

// Add this partial class definition for test visibility
namespace ChattyMcChatface.Api
{
    public partial class Program { }
}
