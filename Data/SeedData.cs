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
        await SeedPermissionsAsync(context);
        await SeedRolePermissionsAsync(context, roleManager);
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

    /// <summary>
    /// تمام دسترسی‌های سیستم را تعریف و seed می‌کند
    /// </summary>
    private static async Task SeedPermissionsAsync(AppDbContext context)
    {
        if (await context.Permissions.AnyAsync())
            return; // Already seeded

        var permissions = new List<Permission>
        {
            // ── داشبورد ──
            new() { Name = "Dashboard.View", DisplayName = "مشاهده داشبورد", Category = "داشبورد", Description = "دسترسی به صفحه اصلی و آمار سیستم" },

            // ── دسته‌بندی‌ها ──
            new() { Name = "Category.View", DisplayName = "مشاهده دسته‌بندی‌ها", Category = "دسته‌بندی‌ها", Description = "مشاهده لیست دسته‌بندی محصولات" },
            new() { Name = "Category.Create", DisplayName = "ایجاد دسته‌بندی", Category = "دسته‌بندی‌ها", Description = "افزودن دسته‌بندی جدید" },
            new() { Name = "Category.Edit", DisplayName = "ویرایش دسته‌بندی", Category = "دسته‌بندی‌ها", Description = "تغییر نام دسته‌بندی" },
            new() { Name = "Category.Delete", DisplayName = "حذف دسته‌بندی", Category = "دسته‌بندی‌ها", Description = "حذف دسته‌بندی (فقط اگر محصول نداشته باشد)" },

            // ── محصولات ──
            new() { Name = "Product.View", DisplayName = "مشاهده محصولات", Category = "محصولات", Description = "مشاهده لیست محصولات و قیمت‌ها" },
            new() { Name = "Product.Create", DisplayName = "ایجاد محصول", Category = "محصولات", Description = "افزودن محصول جدید" },
            new() { Name = "Product.Edit", DisplayName = "ویرایش محصول", Category = "محصولات", Description = "تغییر اطلاعات محصول" },
            new() { Name = "Product.Delete", DisplayName = "حذف محصول", Category = "محصولات", Description = "حذف محصول از سیستم" },
            new() { Name = "Product.ManagePrice", DisplayName = "مدیریت قیمت محصول", Category = "محصولات", Description = "افزودن، ویرایش و بروزرسانی قیمت محصول" },
            new() { Name = "Product.DeletePrice", DisplayName = "حذف قیمت محصول", Category = "محصولات", Description = "حذف رکورد قیمت محصول" },

            // ── اشخاص ──
            new() { Name = "Person.View", DisplayName = "مشاهده اشخاص", Category = "اشخاص", Description = "مشاهده لیست اشخاص حقیقی و حقوقی" },
            new() { Name = "Person.Create", DisplayName = "ایجاد شخص", Category = "اشخاص", Description = "افزودن شخص جدید" },
            new() { Name = "Person.Edit", DisplayName = "ویرایش شخص", Category = "اشخاص", Description = "تغییر اطلاعات شخص" },
            new() { Name = "Person.Delete", DisplayName = "حذف شخص", Category = "اشخاص", Description = "حذف شخص از سیستم" },

            // ── بسته‌ها ──
            new() { Name = "Pack.View", DisplayName = "مشاهده بسته‌ها", Category = "بسته‌ها", Description = "مشاهده لیست بسته‌های محصول" },
            new() { Name = "Pack.Create", DisplayName = "ایجاد بسته", Category = "بسته‌ها", Description = "افزودن بسته جدید و آیتم‌های آن" },
            new() { Name = "Pack.Edit", DisplayName = "ویرایش بسته", Category = "بسته‌ها", Description = "تغییر اطلاعات بسته" },
            new() { Name = "Pack.Delete", DisplayName = "حذف بسته", Category = "بسته‌ها", Description = "حذف بسته و آیتم‌های آن" },

            // ── فاکتورها ──
            new() { Name = "Factor.View", DisplayName = "مشاهده فاکتورها", Category = "فاکتورها", Description = "مشاهده لیست فاکتورها و جزئیات آن‌ها" },
            new() { Name = "Factor.Create", DisplayName = "ایجاد فاکتور", Category = "فاکتورها", Description = "ثبت فاکتور جدید" },
            new() { Name = "Factor.Delete", DisplayName = "حذف فاکتور", Category = "فاکتورها", Description = "حذف فاکتور از سیستم" },
            new() { Name = "Factor.Print", DisplayName = "چاپ فاکتور", Category = "فاکتورها", Description = "چاپ فاکتور به صورت PDF یا Word" },

            // ── گزارش‌ساز آماری ──
            new() { Name = "Report.View", DisplayName = "مشاهده گزارش‌ساز آماری", Category = "گزارش‌ساز آماری", Description = "دسترسی به گزارش‌ساز آماری با نمودارها" },
            new() { Name = "Report.Generate", DisplayName = "تولید گزارش آماری", Category = "گزارش‌ساز آماری", Description = "تولید گزارش و نمودارهای آماری" },

            // ── قالب‌های گزارش ──
            new() { Name = "ReportTemplate.View", DisplayName = "مشاهده قالب‌های گزارش", Category = "قالب‌های گزارش", Description = "مشاهده لیست قالب‌های Word" },
            new() { Name = "ReportTemplate.Manage", DisplayName = "مدیریت قالب‌های گزارش", Category = "قالب‌های گزارش", Description = "آپلود، ویرایش و حذف قالب‌های گزارش Word" },

            // ── مرکز گزارش‌سازی ──
            new() { Name = "ReportBuilder.View", DisplayName = "مشاهده مرکز گزارش‌سازی", Category = "مرکز گزارش‌سازی", Description = "دسترسی به مرکز گزارش‌سازی حرفه‌ای" },
            new() { Name = "ReportBuilder.Generate", DisplayName = "تولید گزارش از مرکز", Category = "مرکز گزارش‌سازی", Description = "تولید گزارش تکی یا گروهی از مرکز گزارش‌سازی" },

            // ── مدیریت کاربران ──
            new() { Name = "User.View", DisplayName = "مشاهده کاربران", Category = "مدیریت کاربران", Description = "مشاهده لیست کاربران و نقش‌ها" },
            new() { Name = "User.Create", DisplayName = "ایجاد کاربر", Category = "مدیریت کاربران", Description = "افزودن کاربر جدید به سیستم" },
            new() { Name = "User.Edit", DisplayName = "ویرایش کاربر", Category = "مدیریت کاربران", Description = "تغییر اطلاعات کاربر، رمز عبور و نقش‌ها" },
            new() { Name = "User.Delete", DisplayName = "حذف کاربر", Category = "مدیریت کاربران", Description = "حذف کاربر از سیستم" },

            // ── مدیریت نقش‌ها ──
            new() { Name = "Role.Manage", DisplayName = "مدیریت نقش‌ها و دسترسی‌ها", Category = "مدیریت نقش‌ها", Description = "ایجاد، حذف نقش و تنظیم دسترسی‌های نقش" },

            // ── تنظیمات ──
            new() { Name = "Settings.View", DisplayName = "مشاهده تنظیمات", Category = "تنظیمات", Description = "دسترسی به صفحه تنظیمات سیستم" },
            new() { Name = "Settings.Edit", DisplayName = "ویرایش تنظیمات", Category = "تنظیمات", Description = "تغییر تنظیمات سیستم" },
        };

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// اختصاص دسترسی‌های پیش‌فرض به نقش‌ها
    /// </summary>
    private static async Task SeedRolePermissionsAsync(AppDbContext context, RoleManager<AppRole> roleManager)
    {
        // If role permissions already exist, don't re-seed
        if (await context.RolePermissions.AnyAsync())
            return;

        var allPermissions = await context.Permissions.ToListAsync();
        var permissionDict = allPermissions.ToDictionary(p => p.Name, p => p.Id);

        // ── Admin: All permissions ──
        var adminRole = await roleManager.FindByNameAsync("Admin");
        if (adminRole != null)
        {
            foreach (var perm in allPermissions)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = perm.Id
                });
            }
        }

        // ── Manager: Factor + Report management ──
        var managerRole = await roleManager.FindByNameAsync("Manager");
        if (managerRole != null)
        {
            var managerPerms = new[]
            {
                "Dashboard.View",
                "Category.View", "Category.Create", "Category.Edit",
                "Product.View", "Product.Create", "Product.Edit", "Product.Delete", "Product.ManagePrice", "Product.DeletePrice",
                "Person.View", "Person.Create", "Person.Edit",
                "Pack.View", "Pack.Create", "Pack.Edit", "Pack.Delete",
                "Factor.View", "Factor.Create", "Factor.Delete", "Factor.Print",
                "Report.View", "Report.Generate",
                "ReportTemplate.View", "ReportTemplate.Manage",
                "ReportBuilder.View", "ReportBuilder.Generate",
            };

            foreach (var permName in managerPerms)
            {
                if (permissionDict.TryGetValue(permName, out int permId))
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = managerRole.Id,
                        PermissionId = permId
                    });
                }
            }
        }

        // ── User: View + Create factors ──
        var userRole = await roleManager.FindByNameAsync("User");
        if (userRole != null)
        {
            var userPerms = new[]
            {
                "Dashboard.View",
                "Category.View",
                "Product.View",
                "Person.View",
                "Pack.View",
                "Factor.View", "Factor.Create", "Factor.Print",
                "ReportBuilder.View",
            };

            foreach (var permName in userPerms)
            {
                if (permissionDict.TryGetValue(permName, out int permId))
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = userRole.Id,
                        PermissionId = permId
                    });
                }
            }
        }

        await context.SaveChangesAsync();
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
