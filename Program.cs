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

        // If an inconsistent database exists (e.g. from old EnsureCreated runs), delete it
        if (await context.Database.CanConnectAsync())
        {
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            if (appliedMigrations.Any())
            {
                var conn = context.Database.GetDbConnection();
                await conn.OpenAsync();
                try
                {
                    using var cmd = conn.CreateCommand();
                    // Check if core table exists when migrations claim to be applied
                    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Users'";
                    var usersTableExists = Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;

                    // Check if AppSettings table exists when the AddAppSettings migration was applied
                    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='AppSettings'";
                    var appSettingsTableExists = Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;

                    var needsReset = false;
                    if (!usersTableExists)
                    {
                        needsReset = true;
                        logger.LogWarning("Inconsistent database detected: Users table missing. Recreating...");
                    }
                    else if (appliedMigrations.Contains("20240612120001_AddAppSettings") && !appSettingsTableExists)
                    {
                        needsReset = true;
                        logger.LogWarning("Inconsistent database detected: AppSettings migration applied but table missing. Recreating...");
                    }

                    if (needsReset)
                    {
                        await conn.CloseAsync();
                        await context.Database.EnsureDeletedAsync();
                    }
                }
                catch
                {
                    logger.LogWarning("Database health check failed. Recreating...");
                    await conn.CloseAsync();
                    await context.Database.EnsureDeletedAsync();
                }
                finally
                {
                    if (conn.State == System.Data.ConnectionState.Open)
                        await conn.CloseAsync();
                }
            }
        }

        await context.Database.MigrateAsync();
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
