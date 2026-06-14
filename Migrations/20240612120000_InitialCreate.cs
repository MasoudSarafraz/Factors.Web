using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Factors.Web.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
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
                UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false),
                FullName = table.Column<string>(type: "TEXT", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastLoginDate = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Roles",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Roles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ProductCategories",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductCategories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ProductPacks",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PackName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                PackCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductPacks", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Persons",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PersonName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                IsIndividual = table.Column<bool>(type: "INTEGER", nullable: false),
                CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Persons", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ReportTemplates",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                TemplateType = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                FilePath = table.Column<string>(type: "TEXT", nullable: false),
                OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReportTemplates", x => x.Id);
            });

        // Indexes for Users
        migrationBuilder.CreateIndex(
            name: "IX_Users_NormalizedEmail",
            table: "Users",
            column: "NormalizedEmail");

        migrationBuilder.CreateIndex(
            name: "IX_Users_NormalizedUserName",
            table: "Users",
            column: "NormalizedUserName",
            unique: true);

        // Indexes for Roles
        migrationBuilder.CreateIndex(
            name: "IX_Roles_NormalizedName",
            table: "Roles",
            column: "NormalizedName",
            unique: true);

        // Indexes for ProductCategories
        migrationBuilder.CreateIndex(
            name: "IX_ProductCategories_Name",
            table: "ProductCategories",
            column: "Name",
            unique: true);

        // Indexes for ProductPacks
        migrationBuilder.CreateIndex(
            name: "IX_ProductPacks_PackCode",
            table: "ProductPacks",
            column: "PackCode",
            unique: true);

        // Products depends on ProductCategories
        migrationBuilder.CreateTable(
            name: "Products",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
                table.ForeignKey(
                    name: "FK_Products_ProductCategories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "ProductCategories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // ProductPrices depends on Products
        migrationBuilder.CreateTable(
            name: "ProductPrices",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                Price = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductPrices", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProductPrices_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ProductPackItems depends on ProductPacks and Products
        migrationBuilder.CreateTable(
            name: "ProductPackItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Qty = table.Column<int>(type: "INTEGER", nullable: false),
                Price = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                PackId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductPackItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProductPackItems_ProductPacks_PackId",
                    column: x => x.PackId,
                    principalTable: "ProductPacks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProductPackItems_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // ReportTemplateMarkers depends on ReportTemplates
        migrationBuilder.CreateTable(
            name: "ReportTemplateMarkers",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                MarkerName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                DataType = table.Column<int>(type: "INTEGER", nullable: false),
                DataSource = table.Column<int>(type: "INTEGER", nullable: false),
                PropertyPath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                ParentListMarker = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReportTemplateMarkers", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReportTemplateMarkers_ReportTemplates_TemplateId",
                    column: x => x.TemplateId,
                    principalTable: "ReportTemplates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Factors depends on Persons and Users
        migrationBuilder.CreateTable(
            name: "Factors",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CreateDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                PersonId = table.Column<int>(type: "INTEGER", nullable: false),
                AppUserId = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Factors", x => x.Id);
                table.ForeignKey(
                    name: "FK_Factors_Persons_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Persons",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Factors_Users_AppUserId",
                    column: x => x.AppUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // FactorItems depends on Factors, Products, ProductPacks
        migrationBuilder.CreateTable(
            name: "FactorItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SalableId = table.Column<int>(type: "INTEGER", nullable: true),
                PackId = table.Column<int>(type: "INTEGER", nullable: true),
                ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                Qty = table.Column<int>(type: "INTEGER", nullable: false),
                Price = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                FactorId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FactorItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_FactorItems_Factors_FactorId",
                    column: x => x.FactorId,
                    principalTable: "Factors",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_FactorItems_Products_SalableId",
                    column: x => x.SalableId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_FactorItems_ProductPacks_PackId",
                    column: x => x.PackId,
                    principalTable: "ProductPacks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Identity relationship tables
        migrationBuilder.CreateTable(
            name: "UserClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<int>(type: "INTEGER", nullable: false),
                ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserClaims_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                UserId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey(
                    name: "FK_UserLogins_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserRoles",
            columns: table => new
            {
                UserId = table.Column<int>(type: "INTEGER", nullable: false),
                RoleId = table.Column<int>(type: "INTEGER", nullable: false)
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

        migrationBuilder.CreateTable(
            name: "RoleClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RoleClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_RoleClaims_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserTokens",
            columns: table => new
            {
                UserId = table.Column<int>(type: "INTEGER", nullable: false),
                LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Value = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey(
                    name: "FK_UserTokens_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Indexes for Products
        migrationBuilder.CreateIndex(
            name: "IX_Products_Code",
            table: "Products",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Products_CategoryId",
            table: "Products",
            column: "CategoryId");

        // Indexes for ProductPrices
        migrationBuilder.CreateIndex(
            name: "IX_ProductPrices_ProductId",
            table: "ProductPrices",
            column: "ProductId");

        // Indexes for ProductPackItems
        migrationBuilder.CreateIndex(
            name: "IX_ProductPackItems_PackId",
            table: "ProductPackItems",
            column: "PackId");

        migrationBuilder.CreateIndex(
            name: "IX_ProductPackItems_ProductId",
            table: "ProductPackItems",
            column: "ProductId");

        // Indexes for Factors
        migrationBuilder.CreateIndex(
            name: "IX_Factors_PersonId",
            table: "Factors",
            column: "PersonId");

        migrationBuilder.CreateIndex(
            name: "IX_Factors_AppUserId",
            table: "Factors",
            column: "AppUserId");

        // Indexes for FactorItems
        migrationBuilder.CreateIndex(
            name: "IX_FactorItems_FactorId",
            table: "FactorItems",
            column: "FactorId");

        migrationBuilder.CreateIndex(
            name: "IX_FactorItems_SalableId",
            table: "FactorItems",
            column: "SalableId");

        migrationBuilder.CreateIndex(
            name: "IX_FactorItems_PackId",
            table: "FactorItems",
            column: "PackId");

        // Indexes for ReportTemplateMarkers
        migrationBuilder.CreateIndex(
            name: "IX_ReportTemplateMarkers_TemplateId",
            table: "ReportTemplateMarkers",
            column: "TemplateId");

        // Indexes for Identity
        migrationBuilder.CreateIndex(
            name: "IX_UserClaims_UserId",
            table: "UserClaims",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserLogins_UserId",
            table: "UserLogins",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_RoleId",
            table: "UserRoles",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "IX_RoleClaims_RoleId",
            table: "RoleClaims",
            column: "RoleId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserTokens");
        migrationBuilder.DropTable(name: "RoleClaims");
        migrationBuilder.DropTable(name: "UserLogins");
        migrationBuilder.DropTable(name: "UserClaims");
        migrationBuilder.DropTable(name: "UserRoles");
        migrationBuilder.DropTable(name: "FactorItems");
        migrationBuilder.DropTable(name: "Factors");
        migrationBuilder.DropTable(name: "ReportTemplateMarkers");
        migrationBuilder.DropTable(name: "ProductPackItems");
        migrationBuilder.DropTable(name: "ProductPrices");
        migrationBuilder.DropTable(name: "Products");
        migrationBuilder.DropTable(name: "ReportTemplates");
        migrationBuilder.DropTable(name: "Persons");
        migrationBuilder.DropTable(name: "ProductPacks");
        migrationBuilder.DropTable(name: "ProductCategories");
        migrationBuilder.DropTable(name: "Roles");
        migrationBuilder.DropTable(name: "Users");
    }
}
