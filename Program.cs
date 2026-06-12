using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// مرحله ۱: ساخت تمام جداول دیتابیس با Raw SQL
// این کار قبل از هر چیز دیگه‌ای انجام میشه تا مطمئن باشیم
// جداول وجود دارن قبل از اینکه EF Core یا Identity بخوان دسترسی داشته باشن
// ============================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
Console.WriteLine("============================================================");
Console.WriteLine("  Factors.Web - Database Initialization");
Console.WriteLine($"  Connection String: {connectionString}");
Console.WriteLine("============================================================");

EnsureDatabaseTables(connectionString);

Console.WriteLine("============================================================");
Console.WriteLine("  Database tables created successfully!");
Console.WriteLine("============================================================");

// Add services to the container
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

// Identity
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "Factors.Auth";
});

builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IReportTemplateService, ReportTemplateService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireManagerRole", policy => policy.RequireRole("Admin", "Manager"));
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

// Seed data - فقط درج داده‌ها (ساخت جداول قبلاً انجام شده)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        await SeedData.InitializeAsync(services);
        logger.LogInformation("Database seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database seeding failed!");
        throw;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ============================================================
// ساخت تمام جداول دیتابیس با Raw SQL
// از Microsoft.Data.Sqlite مستقیم استفاده می‌کنیم
// هیچ وابستگی به EnsureCreatedAsync یا MigrateAsync نداره
// ============================================================
static void EnsureDatabaseTables(string connectionString)
{
    using var conn = new SqliteConnection(connectionString);
    conn.Open();

    // حذف جدول __EFMigrationsHistory اگر وجود داشته باشه
    // این جدول باعث میشه EnsureCreatedAsync کار نکنه
    Execute(conn, "DROP TABLE IF EXISTS __EFMigrationsHistory");

    // ---- Identity Tables ----
    Execute(conn, @"
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
    Console.WriteLine("  [OK] Users");

    Execute(conn, "CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_NormalizedUserName ON Users (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL");
    Execute(conn, "CREATE INDEX IF NOT EXISTS IX_Users_NormalizedEmail ON Users (NormalizedEmail) WHERE NormalizedEmail IS NOT NULL");

    Execute(conn, @"
        CREATE TABLE IF NOT EXISTS Roles (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT,
            NormalizedName TEXT,
            ConcurrencyStamp TEXT,
            Description TEXT NOT NULL DEFAULT '',
            CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
        )");
    Console.WriteLine("  [OK] Roles");

    Execute(conn, "CREATE UNIQUE INDEX IF NOT EXISTS IX_Roles_NormalizedName ON Roles (NormalizedName) WHERE NormalizedName IS NOT NULL");

    Execute(conn, @"
        CREATE TABLE IF NOT EXISTS UserRoles (
            UserId INTEGER NOT NULL,
            RoleId INTEGER NOT NULL,
            PRIMARY KEY (UserId, RoleId),
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
            FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
        )");
    Console.WriteLine("  [OK] UserRoles");

    Execute(conn, "CREATE INDEX IF NOT EXISTS IX_UserRoles_RoleId ON UserRoles (RoleId)");

    Execute(conn, @"
        CREATE TABLE IF NOT EXISTS UserClaims (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            ClaimType TEXT,
            ClaimValue TEXT,
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
        )");
    Console.WriteLine("  [OK] UserClaims");

    Execute(conn, "CREATE INDEX IF NOT EXISTS IX_UserClaims_UserId ON UserClaims (UserId)");

    Execute(conn, @"
        CREATE TABLE IF NOT EXISTS UserLogins (
            LoginProvider TEXT NOT NULL,
            ProviderKey TEXT NOT NULL,
            ProviderDisplayName TEXT,
            UserId INTEGER NOT NULL,
            PRIMARY KEY (LoginProvider, ProviderKey),
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
        )");
    Console.WriteLine("  [OK] UserLogins");

    Execute(conn, "CREATE INDEX IF NOT EXISTS IX_UserLogins_UserId ON UserLogins (UserId)");

    Execute(conn, @"
        CREATE TABLE IF NOT EXISTS RoleClaims (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RoleId INTEGER NOT NULL,
            ClaimType TEXT,
            ClaimValue TEXT,
            FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
        )");
    Console.WriteLine("  [OK] RoleClaims");

    Execute(conn, "CREATE INDEX IF NOT EXISTS IX_RoleClaims_RoleId ON RoleClaims (RoleId)");

    Execute(conn, @"
        CREATE TABLE IF NOT EXISTS UserTokens (
            UserId INTEGER NOT NULL,
            LoginProvider TEXT NOT NULL,
            Name TEXT NOT NULL,
            Value TEXT,
            PRIMARY KEY (UserId, LoginProvider, Name),
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
        )");
    Console.WriteLine("  [OK] UserTokens");

    // ---- Business Tables ----
    Execute(conn, @"
        CREATE TABLE IF NOT EXISTS ProductCategories (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL DEFAULT '',
            CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
        )");
    Console.WriteLine("  [OK] ProductCategories");

    Execute(conn, "CREATE UNIQUE INDEX IF NOT EXISTS IX_ProductCategories_Name ON ProductCategories (Name)");

    Execute(conn, @"
        CREATE TABLE IF NOT EXISTS Products (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL DEFAULT '',
            Code TEXT NOT NULL DEFAULT '',
            CategoryId INTEGER NOT NULL,
            CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
            FOREIGN KEY (CategoryId) REFERENCES ProductCategories(Id) ON DELETE RESTRICT
        )");
    Console.WriteLine("  [OK] Products");

    Execute(conn, "CREATE UNIQUE INDEX IF NOT EXISTS IX_Products_Code ON Products (Code)");

    Execute(conn, @"
        CREATE TABLE IF NOT EXISTS ProductPrices (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            StartTime TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
            EndTime TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
            Price TEXT NOT NULL DEFAULT '0',
            ProductId INTEGER NOT NULL,
            CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
            FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
        )");
    Console.WriteLine("  [OK] ProductPrices");

    Execute(conn, @"
        CREATE TABLE IF NOT EXISTS ProductPacks (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            PackName TEXT NOT NULL DEFAULT '',
            PackCode TEXT NOT NULL DEFAULT '',
            CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
        )");
    Console.WriteLine("  [OK] ProductPacks");

    Execute(conn, "CREATE UNIQUE INDEX IF NOT EXISTS IX_ProductPacks_PackCode ON ProductPacks (PackCode)");

    Execute(conn, @"
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
    Console.WriteLine("  [OK] ProductPackItems");

    Execute(conn, @"
        CREATE TABLE IF NOT EXISTS Persons (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            PersonName TEXT NOT NULL DEFAULT '',
            IsIndividual INTEGER NOT NULL DEFAULT 1,
            CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
        )");
    Console.WriteLine("  [OK] Persons");

    Execute(conn, @"
        CREATE TABLE IF NOT EXISTS Factors (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
            PersonId INTEGER NOT NULL,
            AppUserId INTEGER,
            FOREIGN KEY (PersonId) REFERENCES Persons(Id) ON DELETE RESTRICT,
            FOREIGN KEY (AppUserId) REFERENCES Users(Id) ON DELETE SET NULL
        )");
    Console.WriteLine("  [OK] Factors");

    Execute(conn, "CREATE INDEX IF NOT EXISTS IX_Factors_PersonId ON Factors (PersonId)");
    Execute(conn, "CREATE INDEX IF NOT EXISTS IX_Factors_AppUserId ON Factors (AppUserId)");

    Execute(conn, @"
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
    Console.WriteLine("  [OK] FactorItems");

    Execute(conn, "CREATE INDEX IF NOT EXISTS IX_FactorItems_FactorId ON FactorItems (FactorId)");
    Execute(conn, "CREATE INDEX IF NOT EXISTS IX_FactorItems_SalableId ON FactorItems (SalableId)");
    Execute(conn, "CREATE INDEX IF NOT EXISTS IX_FactorItems_PackId ON FactorItems (PackId)");

    Execute(conn, @"
        CREATE TABLE IF NOT EXISTS ReportTemplates (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL DEFAULT '',
            Description TEXT,
            TemplateType INTEGER NOT NULL DEFAULT 0,
            FilePath TEXT NOT NULL DEFAULT '',
            OriginalFileName TEXT NOT NULL DEFAULT '',
            CreateDate TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
        )");
    Console.WriteLine("  [OK] ReportTemplates");

    Execute(conn, @"
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
    Console.WriteLine("  [OK] ReportTemplateMarkers");

    Execute(conn, "CREATE INDEX IF NOT EXISTS IX_ReportTemplateMarkers_TemplateId ON ReportTemplateMarkers (TemplateId)");

    // ستون‌های جدید که ممکنه در نسخه‌های قدیمی وجود نداشته باشن
    AddColumnIfNotExists(conn, "ReportTemplateMarkers", "ParentListMarker", "TEXT");
    AddColumnIfNotExists(conn, "ReportTemplates", "TemplateType", "INTEGER NOT NULL DEFAULT 0");
    AddColumnIfNotExists(conn, "Factors", "AppUserId", "INTEGER");

    // بررسی نهایی - آیا جدول Users ساخته شده؟
    using var checkCmd = conn.CreateCommand();
    checkCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Users'";
    var userTableExists = Convert.ToInt64(checkCmd.ExecuteScalar()) > 0;

    if (!userTableExists)
    {
        throw new InvalidOperationException("CRITICAL: Users table was not created! Check SQLite connection.");
    }

    Console.WriteLine("  [VERIFIED] Users table exists.");

    conn.Close();
}

static void Execute(SqliteConnection conn, string sql)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    cmd.ExecuteNonQuery();
}

static void AddColumnIfNotExists(SqliteConnection conn, string tableName, string columnName, string columnDef)
{
    var columns = new List<string>();
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = $"PRAGMA table_info({tableName})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }
    }

    if (!columns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDef};";
        cmd.ExecuteNonQuery();
        Console.WriteLine($"  [ADD] {tableName}.{columnName}");
    }
}
