using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChattyMcChatface.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateAndSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    LastName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsPersona = table.Column<bool>(type: "INTEGER", nullable: false),
                    SystemPrompt = table.Column<string>(type: "TEXT", nullable: true),
                    PreferredModelId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Chatrooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PersonaUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chatrooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chatrooms_Users_PersonaUserId",
                        column: x => x.PersonaUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChatroomId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Chatrooms_ChatroomId",
                        column: x => x.ChatroomId,
                        principalTable: "Chatrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatroomUser",
                columns: table => new
                {
                    ChatroomsId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatroomUser", x => new { x.ChatroomsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_ChatroomUser_Chatrooms_ChatroomsId",
                        column: x => x.ChatroomsId,
                        principalTable: "Chatrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatroomUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LastReads",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChatroomId = table.Column<int>(type: "INTEGER", nullable: false),
                    LastReadDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LastReads", x => new { x.UserId, x.ChatroomId });
                    table.ForeignKey(
                        name: "FK_LastReads_Chatrooms_ChatroomId",
                        column: x => x.ChatroomId,
                        principalTable: "Chatrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LastReads_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatroomId",
                table: "ChatMessages",
                column: "ChatroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_UserId",
                table: "ChatMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Chatrooms_PersonaUserId",
                table: "Chatrooms",
                column: "PersonaUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatroomUser_UsersId",
                table: "ChatroomUser",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_LastReads_ChatroomId",
                table: "LastReads",
                column: "ChatroomId");
            // Persona seeding
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "FirstName", "LastName", "Email", "PasswordHash", "IsPersona", "SystemPrompt", "PreferredModelId", "CreatedAt" },
                values: new object[,]
                {
                    {
                        1001, // Id
                        "Persona 1001", // FirstName
                        "System", // LastName
                        "persona1001@system.local", // Email
                        "SYSTEM_GENERATED_NO_LOGIN", // PasswordHash
                        true, // IsPersona
                        "You are a helpful, friendly AI assistant. Always respond in a supportive and informative manner. Provide accurate information and assistance to the user while maintaining a positive and professional tone.", // SystemPrompt for 1001
                        "gemini-2.5-pro-preview-03-25", // PreferredModelId for 1001
                        DateTime.UtcNow // CreatedAt
                    },
                    {
                        1002, // Id
                        "Persona 1002", // FirstName
                        "System", // LastName
                        "persona1002@system.local", // Email
                        "SYSTEM_GENERATED_NO_LOGIN", // PasswordHash
                        true, // IsPersona
                        "You are a sarcastic, witty AI bot. Respond with humor, sarcasm, and a touch of playful mockery while still being helpful. Avoid being mean-spirited but don't be afraid to use irony and clever comebacks.", // SystemPrompt for 1002
                        "gemini-2.5-pro-preview-03-25", // PreferredModelId for 1002
                        DateTime.UtcNow // CreatedAt
                    },
                    {
                        1003, // Id
                        "Persona 1003", // FirstName
                        "System", // LastName
                        "persona1003@system.local", // Email
                        "SYSTEM_GENERATED_NO_LOGIN", // PasswordHash
                        true, // IsPersona
                        "You are a thoughtful AI that ponders the deeper meanings of user messages. Respond with insightful questions, philosophical musings, and encourage reflection. Avoid simple answers; instead, explore the nuances and complexities of the topic.", // SystemPrompt for 1003
                        "gemini-2.5-pro-preview-03-25", // PreferredModelId for 1003
                        DateTime.UtcNow // CreatedAt
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove persona seed data
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValues: new object[] { 1001, 1002, 1003 });

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChatroomUser");

            migrationBuilder.DropTable(
                name: "LastReads");

            migrationBuilder.DropTable(
                name: "Chatrooms");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
