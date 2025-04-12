using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChattyMcChatface.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleToChatMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "ChatMessages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "ChatMessages");
        }
    }
}
