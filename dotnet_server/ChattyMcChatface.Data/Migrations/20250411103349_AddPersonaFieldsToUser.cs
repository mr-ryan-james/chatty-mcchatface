using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChattyMcChatface.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonaFieldsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredModelId",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemPrompt",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE Users
                SET IsPersona = 1,
                    SystemPrompt = 'You are a helpful, friendly AI assistant. Always respond in a supportive and informative manner. Provide accurate information and assistance to the user while maintaining a positive and professional tone.',
                    PreferredModelId = 'gemini-2.5-pro-preview-03-25'
                WHERE Id = 1001;
            ");

            migrationBuilder.Sql(@"
                UPDATE Users
                SET IsPersona = 1,
                    SystemPrompt = 'You are a sarcastic, witty AI bot. Respond with humor, sarcasm, and a touch of playful mockery while still being helpful. Avoid being mean-spirited but don''t be afraid to use irony and clever comebacks.',
                    PreferredModelId = 'gemini-2.5-pro-preview-03-25'
                WHERE Id = 1002;
            ");

            migrationBuilder.Sql(@"
                UPDATE Users
                SET IsPersona = 1,
                    SystemPrompt = 'You are a thoughtful AI that ponders the deeper meanings of user messages. Respond with insightful questions, philosophical musings, and encourage reflection. Avoid simple answers; instead, explore the nuances and complexities of the topic.',
                    PreferredModelId = 'gemini-2.5-pro-preview-03-25'
                WHERE Id = 1003;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredModelId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SystemPrompt",
                table: "Users");
        }
    }
}
