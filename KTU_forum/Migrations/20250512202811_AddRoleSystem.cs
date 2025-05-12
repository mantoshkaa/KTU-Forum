using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace KTU_forum.Migrations
{
    public partial class AddRoleSystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Roles table
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Color = table.Column<string>(type: "text", nullable: true),
                    DisplayPriority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            // Add PrimaryRoleId to Users table
            migrationBuilder.AddColumn<int>(
                name: "PrimaryRoleId",
                table: "Users",
                type: "integer",
                nullable: true);

            // Create UserRoles junction table
            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Add Foreign Key for PrimaryRoleId
            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_PrimaryRoleId",
                table: "Users",
                column: "PrimaryRoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Create index on RoleId in UserRoles table
            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            // Create index on PrimaryRoleId in Users table
            migrationBuilder.CreateIndex(
                name: "IX_Users_PrimaryRoleId",
                table: "Users",
                column: "PrimaryRoleId");

            // Insert default roles
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Name", "Description", "Color", "DisplayPriority" },
                values: new object[,]
                {
                    { "Admin", "Administrator with full permissions", "#FF5733", 1 },
                    { "Newcomer", "New user who hasn't posted yet", "#A7C7E7", 5 },
                    { "Member", "Regular user who has posted at least once", "#77DD77", 4 },
                    { "Regular", "Active member with at least 10 posts", "#FFB347", 3 },
                    { "Senior", "Member for over a month", "#9370DB", 2 },
                    { "Expert", "Knowledgeable member recognized by admins", "#FF6961", 0 }
                });

            // Migrate existing Admin roles to the new role system
            // PostgreSQL uses different SQL syntax
            migrationBuilder.Sql(@"
                DO $$
                DECLARE 
                    admin_role_id INTEGER;
                    newcomer_role_id INTEGER;
                BEGIN
                    SELECT ""Id"" INTO admin_role_id FROM ""Roles"" WHERE ""Name"" = 'Admin';
                    SELECT ""Id"" INTO newcomer_role_id FROM ""Roles"" WHERE ""Name"" = 'Newcomer';
        
                    -- Add admin role to users with 'Admin' in Role column
                    INSERT INTO ""UserRoles"" (""UserId"", ""RoleId"", ""AssignedAt"")
                    SELECT ""Id"", admin_role_id, NOW()
                    FROM ""Users""
                    WHERE ""Role"" = 'Admin';
        
                    -- Set PrimaryRoleId for admins
                    UPDATE ""Users""
                    SET ""PrimaryRoleId"" = admin_role_id
                    WHERE ""Role"" = 'Admin';
        
                    -- Add newcomer role to all non-admin users
                    INSERT INTO ""UserRoles"" (""UserId"", ""RoleId"", ""AssignedAt"")
                    SELECT ""Id"", newcomer_role_id, NOW()
                    FROM ""Users""
                    WHERE ""Role"" != 'Admin' OR ""Role"" IS NULL;
        
                    -- Set PrimaryRoleId for non-admins
                    UPDATE ""Users""
                    SET ""PrimaryRoleId"" = newcomer_role_id
                    WHERE ""Role"" != 'Admin' OR ""Role"" IS NULL;
                END $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop UserRoles table
            migrationBuilder.DropTable(
                name: "UserRoles");

            // Remove foreign key on Users
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_PrimaryRoleId",
                table: "Users");

            // Drop index on PrimaryRoleId
            migrationBuilder.DropIndex(
                name: "IX_Users_PrimaryRoleId",
                table: "Users");

            // Remove PrimaryRoleId column
            migrationBuilder.DropColumn(
                name: "PrimaryRoleId",
                table: "Users");

            // Drop Roles table
            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}