using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Factors.Web.Migrations;

/// <inheritdoc />
public partial class AddRBACPermissions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Permissions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Permissions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RolePermissions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                PermissionId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RolePermissions", x => x.Id);
                table.ForeignKey(
                    name: "FK_RolePermissions_Permissions_PermissionId",
                    column: x => x.PermissionId,
                    principalTable: "Permissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_RolePermissions_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserPermissions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<int>(type: "INTEGER", nullable: false),
                PermissionId = table.Column<int>(type: "INTEGER", nullable: false),
                IsGranted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserPermissions", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserPermissions_Permissions_PermissionId",
                    column: x => x.PermissionId,
                    principalTable: "Permissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserPermissions_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Permissions_Name",
            table: "Permissions",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_PermissionId",
            table: "RolePermissions",
            column: "PermissionId");

        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_RoleId_PermissionId",
            table: "RolePermissions",
            columns: new[] { "RoleId", "PermissionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserPermissions_PermissionId",
            table: "UserPermissions",
            column: "PermissionId");

        migrationBuilder.CreateIndex(
            name: "IX_UserPermissions_UserId_PermissionId",
            table: "UserPermissions",
            columns: new[] { "UserId", "PermissionId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RolePermissions");

        migrationBuilder.DropTable(
            name: "UserPermissions");

        migrationBuilder.DropTable(
            name: "Permissions");
    }
}
