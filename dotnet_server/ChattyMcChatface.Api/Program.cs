using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ChattyMcChatface.Data; // Assuming this is where AppDbContext is
using Microsoft.EntityFrameworkCore; // For UseSqlite, UseNpgsql etc.
using Microsoft.AspNetCore.Authentication.JwtBearer; // For JWT authentication
using Microsoft.IdentityModel.Tokens; // For TokenValidationParameters
using System.Text; // For Encoding
using Microsoft.OpenApi.Models;

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

            app.UseCors("AllowAll"); // Apply CORS policy

            app.UseAuthentication(); // Enable authentication middleware
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}

// Add this partial class definition for test visibility
namespace ChattyMcChatface.Api
{
    public partial class Program { }
}
