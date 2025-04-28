using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KTU_forum.Migrations
{
    /// <inheritdoc />
    public partial class addRoleToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SenderRole",
                table: "Messages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SenderRole",
                table: "Messages");
        }
    }
}
