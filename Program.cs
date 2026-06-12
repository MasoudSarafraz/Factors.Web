using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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

        // Consistency check: if migration history exists but tables are missing,
        // the database is in an inconsistent state (e.g. leftover from EnsureCreated).
        // Delete and let MigrateAsync recreate everything from scratch.
        if (await context.Database.CanConnectAsync())
        {
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            if (appliedMigrations.Any())
            {
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
                try
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('Users','Roles','Products','Factors')";
                    var tableCount = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                    if (tableCount < 4)
                    {
                        logger.LogWarning("Inconsistent database: migration history exists but critical tables are missing. Recreating database...");
                        await connection.CloseAsync();
                        await context.Database.EnsureDeletedAsync();
                    }
                }
                catch
                {
                    logger.LogWarning("Database health check failed. Recreating database...");
                    await connection.CloseAsync();
                    await context.Database.EnsureDeletedAsync();
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        await connection.CloseAsync();
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
