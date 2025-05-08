using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KTU_forum.Migrations
{
    /// <inheritdoc />
    public partial class AddEditsToMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "PrivateMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReplyToId",
                table: "PrivateMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivateMessages_ReplyToId",
                table: "PrivateMessages",
                column: "ReplyToId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrivateMessages_PrivateMessages_ReplyToId",
                table: "PrivateMessages",
                column: "ReplyToId",
                principalTable: "PrivateMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrivateMessages_PrivateMessages_ReplyToId",
                table: "PrivateMessages");

            migrationBuilder.DropIndex(
                name: "IX_PrivateMessages_ReplyToId",
                table: "PrivateMessages");

            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "PrivateMessages");

            migrationBuilder.DropColumn(
                name: "ReplyToId",
                table: "PrivateMessages");
        }
    }
}
