using Factors.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Data;

/// <summary>
/// کلاس مدیریت اولیه‌سازی دیتابیس
/// مسئولیت‌ها: ساخت جداول، بروزرسانی اسکیما، درج داده‌های اولیه
/// </summary>
public class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
        var logger = services.GetRequiredService<ILogger<DatabaseInitializer>>();

        // مرحله ۱: ساخت تمام جداول
        await EnsureTablesAsync(context, logger);

        // مرحله ۲: درج داده‌های اولیه
        await SeedData.SeedAsync(context, userManager, roleManager, logger);
    }

    /// <summary>
    /// ساخت تمام جداول دیتابیس با Raw SQL
    /// از CREATE TABLE IF NOT EXISTS استفاده میشه تا idempotent باشه
    /// </summary>
    private static async Task EnsureTablesAsync(AppDbContext context, ILogger logger)
    {
        var connectionString = context.Database.GetConnectionString()!;
        using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync();

        // حذف __EFMigrationsHistory - این جدول باعث میشه EnsureCreatedAsync کار نکنه
        await ExecuteAsync(conn, "DROP TABLE IF EXISTS __EFMigrationsHistory");

        // ---- Identity Tables ----
        await CreateTableAsync(conn, logger, "Users", @"
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

        await CreateIndexAsync(conn, "CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_NormalizedUserName ON Users (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL");
        await CreateIndexAsync(conn, "CREATE INDEX IF NOT EXISTS IX_Users_NormalizedEmail ON Users (NormalizedEmail) WHERE NormalizedEmail IS NOT NULL");

        await CreateTableAsync(conn, logger, "Roles", @"
            CREATE TABLE IF NOT EXISTS Roles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT,
                NormalizedName TEXT,
                ConcurrencyStamp TEXT,
                Description TEXT NOT NULL DEFAULT '',
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
            )");

        await CreateIndexAsync(conn, "CREATE UNIQUE INDEX IF NOT EXISTS IX_Roles_NormalizedName ON Roles (NormalizedName) WHERE NormalizedName IS NOT NULL");

        await CreateTableAsync(conn, logger, "UserRoles", @"
            CREATE TABLE IF NOT EXISTS UserRoles (
                UserId INTEGER NOT NULL,
                RoleId INTEGER NOT NULL,
                PRIMARY KEY (UserId, RoleId),
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
            )");

        await CreateIndexAsync(conn, "CREATE INDEX IF NOT EXISTS IX_UserRoles_RoleId ON UserRoles (RoleId)");

        await CreateTableAsync(conn, logger, "UserClaims", @"
            CREATE TABLE IF NOT EXISTS UserClaims (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                ClaimType TEXT,
                ClaimValue TEXT,
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            )");

        await CreateIndexAsync(conn, "CREATE INDEX IF NOT EXISTS IX_UserClaims_UserId ON UserClaims (UserId)");

        await CreateTableAsync(conn, logger, "UserLogins", @"
            CREATE TABLE IF NOT EXISTS UserLogins (
                LoginProvider TEXT NOT NULL,
                ProviderKey TEXT NOT NULL,
                ProviderDisplayName TEXT,
                UserId INTEGER NOT NULL,
                PRIMARY KEY (LoginProvider, ProviderKey),
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            )");

        await CreateIndexAsync(conn, "CREATE INDEX IF NOT EXISTS IX_UserLogins_UserId ON UserLogins (UserId)");

        await CreateTableAsync(conn, logger, "RoleClaims", @"
            CREATE TABLE IF NOT EXISTS RoleClaims (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RoleId INTEGER NOT NULL,
                ClaimType TEXT,
                ClaimValue TEXT,
                FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
            )");

        await CreateIndexAsync(conn, "CREATE INDEX IF NOT EXISTS IX_RoleClaims_RoleId ON RoleClaims (RoleId)");

        await CreateTableAsync(conn, logger, "UserTokens", @"
            CREATE TABLE IF NOT EXISTS UserTokens (
                UserId INTEGER NOT NULL,
                LoginProvider TEXT NOT NULL,
                Name TEXT NOT NULL,
                Value TEXT,
                PRIMARY KEY (UserId, LoginProvider, Name),
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            )");

        // ---- Business Tables ----
        await CreateTableAsync(conn, logger, "ProductCategories", @"
            CREATE TABLE IF NOT EXISTS ProductCategories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL DEFAULT '',
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
            )");

        await CreateIndexAsync(conn, "CREATE UNIQUE INDEX IF NOT EXISTS IX_ProductCategories_Name ON ProductCategories (Name)");

        await CreateTableAsync(conn, logger, "Products", @"
            CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL DEFAULT '',
                Code TEXT NOT NULL DEFAULT '',
                CategoryId INTEGER NOT NULL,
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                FOREIGN KEY (CategoryId) REFERENCES ProductCategories(Id) ON DELETE RESTRICT
            )");

        await CreateIndexAsync(conn, "CREATE UNIQUE INDEX IF NOT EXISTS IX_Products_Code ON Products (Code)");

        await CreateTableAsync(conn, logger, "ProductPrices", @"
            CREATE TABLE IF NOT EXISTS ProductPrices (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StartTime TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                EndTime TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                Price TEXT NOT NULL DEFAULT '0',
                ProductId INTEGER NOT NULL,
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
            )");

        await CreateTableAsync(conn, logger, "ProductPacks", @"
            CREATE TABLE IF NOT EXISTS ProductPacks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PackName TEXT NOT NULL DEFAULT '',
                PackCode TEXT NOT NULL DEFAULT '',
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
            )");

        await CreateIndexAsync(conn, "CREATE UNIQUE INDEX IF NOT EXISTS IX_ProductPacks_PackCode ON ProductPacks (PackCode)");

        await CreateTableAsync(conn, logger, "ProductPackItems", @"
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

        await CreateTableAsync(conn, logger, "Persons", @"
            CREATE TABLE IF NOT EXISTS Persons (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PersonName TEXT NOT NULL DEFAULT '',
                IsIndividual INTEGER NOT NULL DEFAULT 1,
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
            )");

        await CreateTableAsync(conn, logger, "Factors", @"
            CREATE TABLE IF NOT EXISTS Factors (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                PersonId INTEGER NOT NULL,
                AppUserId INTEGER,
                FOREIGN KEY (PersonId) REFERENCES Persons(Id) ON DELETE RESTRICT,
                FOREIGN KEY (AppUserId) REFERENCES Users(Id) ON DELETE SET NULL
            )");

        await CreateIndexAsync(conn, "CREATE INDEX IF NOT EXISTS IX_Factors_PersonId ON Factors (PersonId)");
        await CreateIndexAsync(conn, "CREATE INDEX IF NOT EXISTS IX_Factors_AppUserId ON Factors (AppUserId)");

        await CreateTableAsync(conn, logger, "FactorItems", @"
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

        await CreateIndexAsync(conn, "CREATE INDEX IF NOT EXISTS IX_FactorItems_FactorId ON FactorItems (FactorId)");
        await CreateIndexAsync(conn, "CREATE INDEX IF NOT EXISTS IX_FactorItems_SalableId ON FactorItems (SalableId)");
        await CreateIndexAsync(conn, "CREATE INDEX IF NOT EXISTS IX_FactorItems_PackId ON FactorItems (PackId)");

        await CreateTableAsync(conn, logger, "ReportTemplates", @"
            CREATE TABLE IF NOT EXISTS ReportTemplates (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL DEFAULT '',
                Description TEXT,
                TemplateType INTEGER NOT NULL DEFAULT 0,
                FilePath TEXT NOT NULL DEFAULT '',
                OriginalFileName TEXT NOT NULL DEFAULT '',
                CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
            )");

        await CreateTableAsync(conn, logger, "ReportTemplateMarkers", @"
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

        await CreateIndexAsync(conn, "CREATE INDEX IF NOT EXISTS IX_ReportTemplateMarkers_TemplateId ON ReportTemplateMarkers (TemplateId)");

        // ---- Schema evolution: add missing columns from older versions ----
        await AddColumnIfNotExistsAsync(conn, logger, "ReportTemplateMarkers", "ParentListMarker", "TEXT");
        await AddColumnIfNotExistsAsync(conn, logger, "ReportTemplates", "TemplateType", "INTEGER NOT NULL DEFAULT 0");
        await AddColumnIfNotExistsAsync(conn, logger, "Factors", "AppUserId", "INTEGER");

        await conn.CloseAsync();

        logger.LogInformation("All database tables ensured successfully.");
    }

    // ---- Helper Methods ----

    private static async Task ExecuteAsync(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task CreateTableAsync(SqliteConnection conn, ILogger logger, string tableName, string sql)
    {
        await ExecuteAsync(conn, sql);
        logger.LogDebug("Table {Table} ensured.", tableName);
    }

    private static async Task CreateIndexAsync(SqliteConnection conn, string sql)
    {
        await ExecuteAsync(conn, sql);
    }

    private static async Task AddColumnIfNotExistsAsync(SqliteConnection conn, ILogger logger, string tableName, string columnName, string columnDef)
    {
        var columns = new List<string>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"PRAGMA table_info({tableName})";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                columns.Add(reader.GetString(1));
        }

        if (!columns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDef};";
            await cmd.ExecuteNonQueryAsync();
            logger.LogInformation("Column {Table}.{Column} added.", tableName, columnName);
        }
    }
}

/// <summary>
/// کلاس درج داده‌های اولیه
/// </summary>
public static class SeedData
{
    public static async Task SeedAsync(AppDbContext context, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, ILogger logger)
    {
        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
        await SeedSampleDataAsync(context);
        logger.LogInformation("Database seeded successfully.");
    }

    private static async Task SeedRolesAsync(RoleManager<AppRole> roleManager)
    {
        var roles = new List<AppRole>
        {
            new() { Name = "Admin", Description = "دسترسی کامل به تمام بخش‌ها", CreateDate = DateTime.UtcNow },
            new() { Name = "Manager", Description = "مدیریت فاکتورها و گزارش‌ها", CreateDate = DateTime.UtcNow },
            new() { Name = "User", Description = "فقط مشاهده و ایجاد فاکتور", CreateDate = DateTime.UtcNow }
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name))
                await roleManager.CreateAsync(role);
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<AppUser> userManager)
    {
        if (await userManager.FindByEmailAsync("admin@factors.ir") == null)
        {
            var admin = new AppUser
            {
                UserName = "admin",
                Email = "admin@factors.ir",
                FullName = "مدیر سیستم",
                IsActive = true,
                CreateDate = DateTime.UtcNow,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        if (await userManager.FindByEmailAsync("manager@factors.ir") == null)
        {
            var manager = new AppUser
            {
                UserName = "manager",
                Email = "manager@factors.ir",
                FullName = "مدیر فروش",
                IsActive = true,
                CreateDate = DateTime.UtcNow,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(manager, "Manager@123");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(manager, "Manager");
        }
    }

    private static async Task SeedSampleDataAsync(AppDbContext context)
    {
        if (!await context.ProductCategories.AnyAsync())
        {
            await context.ProductCategories.AddRangeAsync(
                new ProductCategory { Name = "لوازم الکترونیکی", CreateDate = DateTime.UtcNow },
                new ProductCategory { Name = "لوازم خانگی", CreateDate = DateTime.UtcNow },
                new ProductCategory { Name = "قطعات کامپیوتر", CreateDate = DateTime.UtcNow },
                new ProductCategory { Name = "لوازم جانبی", CreateDate = DateTime.UtcNow },
                new ProductCategory { Name = "نرم‌افزار", CreateDate = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.Products.AnyAsync())
        {
            var cat1 = await context.ProductCategories.FirstAsync(c => c.Name == "لوازم الکترونیکی");
            var cat2 = await context.ProductCategories.FirstAsync(c => c.Name == "لوازم خانگی");
            var cat3 = await context.ProductCategories.FirstAsync(c => c.Name == "قطعات کامپیوتر");

            await context.Products.AddRangeAsync(
                new Product { Name = "لپ‌تاپ Dell", Code = "1001", CategoryId = cat1.Id, CreateDate = DateTime.UtcNow },
                new Product { Name = "مانیتور Samsung", Code = "1002", CategoryId = cat1.Id, CreateDate = DateTime.UtcNow },
                new Product { Name = "کیبورد Logitech", Code = "2001", CategoryId = cat3.Id, CreateDate = DateTime.UtcNow },
                new Product { Name = "ماوس Razer", Code = "2002", CategoryId = cat3.Id, CreateDate = DateTime.UtcNow },
                new Product { Name = "یخچال LG", Code = "3001", CategoryId = cat2.Id, CreateDate = DateTime.UtcNow },
                new Product { Name = "ماشین لباسشویی Bosch", Code = "3002", CategoryId = cat2.Id, CreateDate = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var prod1 = await context.Products.FirstAsync(p => p.Code == "1001");
            var prod2 = await context.Products.FirstAsync(p => p.Code == "1002");
            var prod3 = await context.Products.FirstAsync(p => p.Code == "2001");

            await context.ProductPrices.AddRangeAsync(
                new ProductPrice { ProductId = prod1.Id, Price = 45000000, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddMonths(6), CreateDate = DateTime.UtcNow },
                new ProductPrice { ProductId = prod2.Id, Price = 12000000, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddMonths(6), CreateDate = DateTime.UtcNow },
                new ProductPrice { ProductId = prod3.Id, Price = 2500000, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddMonths(6), CreateDate = DateTime.UtcNow }
            );
        }

        if (!await context.Persons.AnyAsync())
        {
            await context.Persons.AddRangeAsync(
                new Person { PersonName = "علی محمدی", IsIndividual = true, CreateDate = DateTime.UtcNow },
                new Person { PersonName = "شرکت فناوری آریا", IsIndividual = false, CreateDate = DateTime.UtcNow },
                new Person { PersonName = "زهرا احمدی", IsIndividual = true, CreateDate = DateTime.UtcNow },
                new Person { PersonName = "فروشگاه دیجیتال پارس", IsIndividual = false, CreateDate = DateTime.UtcNow },
                new Person { PersonName = "مهدی رضایی", IsIndividual = true, CreateDate = DateTime.UtcNow },
                new Person { PersonName = "شرکت بازرگانی نوین", IsIndividual = false, CreateDate = DateTime.UtcNow }
            );
        }

        if (!await context.ProductPacks.AnyAsync())
        {
            await context.ProductPacks.AddRangeAsync(
                new ProductPack { PackName = "بسته اداری", PackCode = "PK001", CreateDate = DateTime.UtcNow },
                new ProductPack { PackName = "بسته خانگی", PackCode = "PK002", CreateDate = DateTime.UtcNow },
                new ProductPack { PackName = "بسته گیمینگ", PackCode = "PK003", CreateDate = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var pack1 = await context.ProductPacks.FirstAsync(p => p.PackCode == "PK001");
            var pack2 = await context.ProductPacks.FirstAsync(p => p.PackCode == "PK002");
            var prod1 = await context.Products.FirstAsync(p => p.Code == "2001");
            var prod2 = await context.Products.FirstAsync(p => p.Code == "2002");
            var prod3 = await context.Products.FirstAsync(p => p.Code == "3001");

            await context.ProductPackItems.AddRangeAsync(
                new ProductPackItems { PackId = pack1.Id, ProductId = prod1.Id, Qty = 1, Price = 2500000, CreateDate = DateTime.UtcNow },
                new ProductPackItems { PackId = pack1.Id, ProductId = prod2.Id, Qty = 1, Price = 3500000, CreateDate = DateTime.UtcNow },
                new ProductPackItems { PackId = pack2.Id, ProductId = prod3.Id, Qty = 1, Price = 35000000, CreateDate = DateTime.UtcNow }
            );
        }

        await context.SaveChangesAsync();
    }
}
