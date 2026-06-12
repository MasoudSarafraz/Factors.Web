using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Factors.Web.Migrations;

/// <inheritdoc />
[Migration("20240612120000_InitialCreate")]
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("PRAGMA journal_mode = 'wal';");

        // ---- Identity Tables ----
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserName TEXT,
                NormalizedUserName TEXT,
                Email TEXT,
                NormalizedEmail TEXT,
                EmailConfirmed INTEGER NOT NULL DEFAULT 0,
                PasswordHash TEXT,
                SecurityStamp TEXT,
                ConcurrencyStamp TEXT,
                PhoneNumber TEXT,
                PhoneNumberConfirmed INTEGER NOT NULL DEFAULT 0,
                TwoFactorEnabled INTEGER NOT NULL DEFAULT 0,
                LockoutEnd TEXT,
                LockoutEnabled INTEGER NOT NULL DEFAULT 0,
                AccessFailedCount INTEGER NOT NULL DEFAULT 0,
                FullName TEXT NOT NULL DEFAULT '',
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                LastLoginDate TEXT
            )");

        migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_NormalizedUserName ON Users (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Users_NormalizedEmail ON Users (NormalizedEmail) WHERE NormalizedEmail IS NOT NULL");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS Roles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT,
                NormalizedName TEXT,
                ConcurrencyStamp TEXT,
                Description TEXT NOT NULL DEFAULT '',
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
            )");

        migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS IX_Roles_NormalizedName ON Roles (NormalizedName) WHERE NormalizedName IS NOT NULL");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS UserRoles (
                UserId INTEGER NOT NULL,
                RoleId INTEGER NOT NULL,
                PRIMARY KEY (UserId, RoleId),
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
            )");

        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_UserRoles_RoleId ON UserRoles (RoleId)");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS UserClaims (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                ClaimType TEXT,
                ClaimValue TEXT,
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            )");

        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_UserClaims_UserId ON UserClaims (UserId)");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS UserLogins (
                LoginProvider TEXT NOT NULL,
                ProviderKey TEXT NOT NULL,
                ProviderDisplayName TEXT,
                UserId INTEGER NOT NULL,
                PRIMARY KEY (LoginProvider, ProviderKey),
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            )");

        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_UserLogins_UserId ON UserLogins (UserId)");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS RoleClaims (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RoleId INTEGER NOT NULL,
                ClaimType TEXT,
                ClaimValue TEXT,
                FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
            )");

        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_RoleClaims_RoleId ON RoleClaims (RoleId)");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS UserTokens (
                UserId INTEGER NOT NULL,
                LoginProvider TEXT NOT NULL,
                Name TEXT NOT NULL,
                Value TEXT,
                PRIMARY KEY (UserId, LoginProvider, Name),
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            )");

        // ---- Business Tables ----
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ProductCategories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL DEFAULT '',
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
            )");

        migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS IX_ProductCategories_Name ON ProductCategories (Name)");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL DEFAULT '',
                Code TEXT NOT NULL DEFAULT '',
                CategoryId INTEGER NOT NULL,
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                FOREIGN KEY (CategoryId) REFERENCES ProductCategories(Id) ON DELETE RESTRICT
            )");

        migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS IX_Products_Code ON Products (Code)");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ProductPrices (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StartTime TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                EndTime TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                Price TEXT NOT NULL DEFAULT '0',
                ProductId INTEGER NOT NULL,
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
            )");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ProductPacks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PackName TEXT NOT NULL DEFAULT '',
                PackCode TEXT NOT NULL DEFAULT '',
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
            )");

        migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS IX_ProductPacks_PackCode ON ProductPacks (PackCode)");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ProductPackItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Qty INTEGER NOT NULL DEFAULT 0,
                Price TEXT NOT NULL DEFAULT '0',
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                ProductId INTEGER NOT NULL,
                PackId INTEGER NOT NULL,
                FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE RESTRICT,
                FOREIGN KEY (PackId) REFERENCES ProductPacks(Id) ON DELETE CASCADE
            )");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS Persons (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PersonName TEXT NOT NULL DEFAULT '',
                IsIndividual INTEGER NOT NULL DEFAULT 1,
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
            )");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS Factors (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                PersonId INTEGER NOT NULL,
                AppUserId INTEGER,
                FOREIGN KEY (PersonId) REFERENCES Persons(Id) ON DELETE RESTRICT,
                FOREIGN KEY (AppUserId) REFERENCES Users(Id) ON DELETE SET NULL
            )");

        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Factors_PersonId ON Factors (PersonId)");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Factors_AppUserId ON Factors (AppUserId)");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS FactorItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SalableId INTEGER,
                PackId INTEGER,
                ParentId INTEGER,
                Qty INTEGER NOT NULL DEFAULT 0,
                Price TEXT NOT NULL DEFAULT '0',
                FactorId INTEGER NOT NULL,
                FOREIGN KEY (FactorId) REFERENCES Factors(Id) ON DELETE CASCADE,
                FOREIGN KEY (SalableId) REFERENCES Products(Id) ON DELETE SET NULL,
                FOREIGN KEY (PackId) REFERENCES ProductPacks(Id) ON DELETE SET NULL
            )");

        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_FactorItems_FactorId ON FactorItems (FactorId)");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_FactorItems_SalableId ON FactorItems (SalableId)");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_FactorItems_PackId ON FactorItems (PackId)");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ReportTemplates (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL DEFAULT '',
                Description TEXT,
                TemplateType INTEGER NOT NULL DEFAULT 0,
                FilePath TEXT NOT NULL DEFAULT '',
                OriginalFileName TEXT NOT NULL DEFAULT '',
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
            )");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ReportTemplateMarkers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TemplateId INTEGER NOT NULL,
                MarkerName TEXT NOT NULL DEFAULT '',
                DataType INTEGER NOT NULL DEFAULT 0,
                DataSource INTEGER NOT NULL DEFAULT 0,
                PropertyPath TEXT,
                ParentListMarker TEXT,
                FOREIGN KEY (TemplateId) REFERENCES ReportTemplates(Id) ON DELETE CASCADE
            )");

        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_ReportTemplateMarkers_TemplateId ON ReportTemplateMarkers (TemplateId)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS ReportTemplateMarkers");
        migrationBuilder.Sql("DROP TABLE IF EXISTS ReportTemplates");
        migrationBuilder.Sql("DROP TABLE IF EXISTS FactorItems");
        migrationBuilder.Sql("DROP TABLE IF EXISTS Factors");
        migrationBuilder.Sql("DROP TABLE IF EXISTS ProductPackItems");
        migrationBuilder.Sql("DROP TABLE IF EXISTS ProductPacks");
        migrationBuilder.Sql("DROP TABLE IF EXISTS ProductPrices");
        migrationBuilder.Sql("DROP TABLE IF EXISTS Products");
        migrationBuilder.Sql("DROP TABLE IF EXISTS ProductCategories");
        migrationBuilder.Sql("DROP TABLE IF EXISTS Persons");
        migrationBuilder.Sql("DROP TABLE IF EXISTS UserTokens");
        migrationBuilder.Sql("DROP TABLE IF EXISTS RoleClaims");
        migrationBuilder.Sql("DROP TABLE IF EXISTS UserLogins");
        migrationBuilder.Sql("DROP TABLE IF EXISTS UserClaims");
        migrationBuilder.Sql("DROP TABLE IF EXISTS UserRoles");
        migrationBuilder.Sql("DROP TABLE IF EXISTS Roles");
        migrationBuilder.Sql("DROP TABLE IF EXISTS Users");
    }
}
