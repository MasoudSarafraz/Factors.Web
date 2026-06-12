using Factors.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
        await SeedSampleDataAsync(context);
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
