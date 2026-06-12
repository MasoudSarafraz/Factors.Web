using Factors.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Data;

/// <summary>
/// نسخه ۵ - فقط درج داده‌های اولیه
/// ساخت جداول حالا در Program.cs انجام میشه با Raw SQL
/// این فایل فقط داده‌ها رو درج می‌کنه
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

        // درج داده‌های اولیه
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
            {
                await roleManager.CreateAsync(role);
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<AppUser> userManager)
    {
        var adminEmail = "admin@factors.ir";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = "admin",
                Email = adminEmail,
                FullName = "مدیر سیستم",
                IsActive = true,
                CreateDate = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        var managerEmail = "manager@factors.ir";
        var managerUser = await userManager.FindByEmailAsync(managerEmail);

        if (managerUser == null)
        {
            managerUser = new AppUser
            {
                UserName = "manager",
                Email = managerEmail,
                FullName = "مدیر فروش",
                IsActive = true,
                CreateDate = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(managerUser, "Manager@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, "Manager");
            }
        }
    }

    private static async Task SeedSampleDataAsync(AppDbContext context)
    {
        if (!await context.ProductCategories.AnyAsync())
        {
            var categories = new List<ProductCategory>
            {
                new() { Name = "لوازم الکترونیکی", CreateDate = DateTime.UtcNow },
                new() { Name = "لوازم خانگی", CreateDate = DateTime.UtcNow },
                new() { Name = "قطعات کامپیوتر", CreateDate = DateTime.UtcNow },
                new() { Name = "لوازم جانبی", CreateDate = DateTime.UtcNow },
                new() { Name = "نرم‌افزار", CreateDate = DateTime.UtcNow }
            };
            await context.ProductCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        if (!await context.Products.AnyAsync())
        {
            var cat1 = await context.ProductCategories.FirstAsync(c => c.Name == "لوازم الکترونیکی");
            var cat2 = await context.ProductCategories.FirstAsync(c => c.Name == "لوازم خانگی");
            var cat3 = await context.ProductCategories.FirstAsync(c => c.Name == "قطعات کامپیوتر");

            var products = new List<Product>
            {
                new() { Name = "لپ‌تاپ Dell", Code = "1001", CategoryId = cat1.Id, CreateDate = DateTime.UtcNow },
                new() { Name = "مانیتور Samsung", Code = "1002", CategoryId = cat1.Id, CreateDate = DateTime.UtcNow },
                new() { Name = "کیبورد Logitech", Code = "2001", CategoryId = cat3.Id, CreateDate = DateTime.UtcNow },
                new() { Name = "ماوس Razer", Code = "2002", CategoryId = cat3.Id, CreateDate = DateTime.UtcNow },
                new() { Name = "یخچال LG", Code = "3001", CategoryId = cat2.Id, CreateDate = DateTime.UtcNow },
                new() { Name = "ماشین لباسشویی Bosch", Code = "3002", CategoryId = cat2.Id, CreateDate = DateTime.UtcNow }
            };
            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            var prod1 = await context.Products.FirstAsync(p => p.Code == "1001");
            var prod2 = await context.Products.FirstAsync(p => p.Code == "1002");
            var prod3 = await context.Products.FirstAsync(p => p.Code == "2001");

            var prices = new List<ProductPrice>
            {
                new() { ProductId = prod1.Id, Price = 45000000, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddMonths(6), CreateDate = DateTime.UtcNow },
                new() { ProductId = prod2.Id, Price = 12000000, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddMonths(6), CreateDate = DateTime.UtcNow },
                new() { ProductId = prod3.Id, Price = 2500000, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddMonths(6), CreateDate = DateTime.UtcNow }
            };
            await context.ProductPrices.AddRangeAsync(prices);
        }

        if (!await context.Persons.AnyAsync())
        {
            var persons = new List<Person>
            {
                new() { PersonName = "علی محمدی", IsIndividual = true, CreateDate = DateTime.UtcNow },
                new() { PersonName = "شرکت فناوری آریا", IsIndividual = false, CreateDate = DateTime.UtcNow },
                new() { PersonName = "زهرا احمدی", IsIndividual = true, CreateDate = DateTime.UtcNow },
                new() { PersonName = "فروشگاه دیجیتال پارس", IsIndividual = false, CreateDate = DateTime.UtcNow },
                new() { PersonName = "مهدی رضایی", IsIndividual = true, CreateDate = DateTime.UtcNow },
                new() { PersonName = "شرکت بازرگانی نوین", IsIndividual = false, CreateDate = DateTime.UtcNow }
            };
            await context.Persons.AddRangeAsync(persons);
        }

        if (!await context.ProductPacks.AnyAsync())
        {
            var packs = new List<ProductPack>
            {
                new() { PackName = "بسته اداری", PackCode = "PK001", CreateDate = DateTime.UtcNow },
                new() { PackName = "بسته خانگی", PackCode = "PK002", CreateDate = DateTime.UtcNow },
                new() { PackName = "بسته گیمینگ", PackCode = "PK003", CreateDate = DateTime.UtcNow }
            };
            await context.ProductPacks.AddRangeAsync(packs);
            await context.SaveChangesAsync();

            var pack1 = await context.ProductPacks.FirstAsync(p => p.PackCode == "PK001");
            var pack2 = await context.ProductPacks.FirstAsync(p => p.PackCode == "PK002");
            var prod1 = await context.Products.FirstAsync(p => p.Code == "2001");
            var prod2 = await context.Products.FirstAsync(p => p.Code == "2002");
            var prod3 = await context.Products.FirstAsync(p => p.Code == "3001");

            var packItems = new List<ProductPackItems>
            {
                new() { PackId = pack1.Id, ProductId = prod1.Id, Qty = 1, Price = 2500000, CreateDate = DateTime.UtcNow },
                new() { PackId = pack1.Id, ProductId = prod2.Id, Qty = 1, Price = 3500000, CreateDate = DateTime.UtcNow },
                new() { PackId = pack2.Id, ProductId = prod3.Id, Qty = 1, Price = 35000000, CreateDate = DateTime.UtcNow }
            };
            await context.ProductPackItems.AddRangeAsync(packItems);
        }

        await context.SaveChangesAsync();
    }
}
