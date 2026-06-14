using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

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
    options.LogoutPath = "/Account/LogoutGet";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "Factors.Auth";
});

builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IReportTemplateService, ReportTemplateService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireManagerRole", policy => policy.RequireRole("Admin", "Manager"));
});

builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN"; });

var app = builder.Build();

// Apply migrations & seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var conn = context.Database.GetDbConnection();

        // Step 1: Check if database exists and is accessible
        if (await context.Database.CanConnectAsync())
        {
            await conn.OpenAsync();
            try
            {
                // Step 2: Check if __EFMigrationsHistory table exists
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
                var historyExists = Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;

                if (!historyExists)
                {
                    // Database was created via EnsureCreated (no migration history)
                    // Stamp all existing migrations as already applied so MigrateAsync won't re-run them
                    logger.LogInformation("Database exists without migration history. Stamping initial migrations...");

                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    if (pendingMigrations.Any())
                    {
                        // Insert migration history records for all pending migrations
                        foreach (var migration in pendingMigrations)
                        {
                            using var insertCmd = conn.CreateCommand();
                            insertCmd.CommandText = $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{migration}', '8.0.11')";
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                        logger.LogInformation("Stamped {Count} migrations as applied.", pendingMigrations.Count());
                    }
                }
                else
                {
                    // Migration history exists - check for consistency
                    var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
                    if (appliedMigrations.Any())
                    {
                        // Verify core tables exist
                        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Users'";
                        var usersTableExists = Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;

                        if (!usersTableExists)
                        {
                            // Inconsistent state - tables missing but migrations claim to be applied
                            // Clear the migration history so MigrateAsync will re-apply everything
                            logger.LogWarning("Inconsistent database detected: Users table missing. Clearing migration history for re-apply...");
                            using var clearCmd = conn.CreateCommand();
                            clearCmd.CommandText = "DELETE FROM __EFMigrationsHistory";
                            await clearCmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Database health check encountered an issue. Will attempt migration anyway.");
            }
            finally
            {
                if (conn.State == System.Data.ConnectionState.Open)
                    await conn.CloseAsync();
            }
        }

        // Step 3: Apply any pending migrations (this creates/updates tables as needed)
        await context.Database.MigrateAsync();

        // Step 4: Seed data
        await SeedData.InitializeAsync(services);
        logger.LogInformation("Database initialized successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database initialization failed!");
        throw;
    }
}

// Pipeline
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

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
