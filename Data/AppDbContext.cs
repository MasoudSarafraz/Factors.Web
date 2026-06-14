using Factors.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Data;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductPrice> ProductPrices { get; set; }
    public DbSet<ProductPack> ProductPacks { get; set; }
    public DbSet<ProductPackItems> ProductPackItems { get; set; }
    public DbSet<Person> Persons { get; set; }
    public DbSet<Factor> Factors { get; set; }
    public DbSet<FactorItems> FactorItems { get; set; }
    public DbSet<ReportTemplate> ReportTemplates { get; set; }
    public DbSet<ReportTemplateMarker> ReportTemplateMarkers { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity table names
        builder.Entity<AppUser>(e => e.ToTable("Users"));
        builder.Entity<AppRole>(e => e.ToTable("Roles"));
        builder.Entity<IdentityUserRole<int>>(e => e.ToTable("UserRoles"));
        builder.Entity<IdentityUserClaim<int>>(e => e.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<int>>(e => e.ToTable("UserLogins"));
        builder.Entity<IdentityRoleClaim<int>>(e => e.ToTable("RoleClaims"));
        builder.Entity<IdentityUserToken<int>>(e => e.ToTable("UserTokens"));

        // ProductCategory
        builder.Entity<ProductCategory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasMany(x => x.Products)
             .WithOne(x => x.Category)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Product
        builder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasMany(x => x.ProductPrices)
             .WithOne(x => x.Product)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.PackItems)
             .WithOne(x => x.Product)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ProductPrice
        builder.Entity<ProductPrice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Price).HasColumnType("DECIMAL(18,2)");
        });

        // ProductPack
        builder.Entity<ProductPack>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PackName).IsRequired().HasMaxLength(100);
            e.Property(x => x.PackCode).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.PackCode).IsUnique();
            e.HasMany(x => x.PackItems)
             .WithOne(x => x.Pack)
             .HasForeignKey(x => x.PackId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductPackItems
        builder.Entity<ProductPackItems>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Price).HasColumnType("DECIMAL(18,2)");
        });

        // Person
        builder.Entity<Person>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PersonName).IsRequired().HasMaxLength(150);
            e.HasMany(x => x.Factors)
             .WithOne(x => x.Person)
             .HasForeignKey(x => x.PersonId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Factor
        builder.Entity<Factor>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.FactorItems)
             .WithOne(x => x.Factor)
             .HasForeignKey(x => x.FactorId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // FactorItems
        builder.Entity<FactorItems>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Price).HasColumnType("DECIMAL(18,2)");
        });

        // ReportTemplate
        builder.Entity<ReportTemplate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.FilePath).IsRequired();
            e.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(300);
            e.Property(x => x.TemplateType).HasDefaultValue(ReportTemplateType.SingleFactor);
            e.HasMany(x => x.Markers)
             .WithOne(x => x.Template)
             .HasForeignKey(x => x.TemplateId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ReportTemplateMarker
        builder.Entity<ReportTemplateMarker>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.MarkerName).IsRequired().HasMaxLength(100);
            e.Property(x => x.PropertyPath).HasMaxLength(200);
            e.Property(x => x.ParentListMarker).HasMaxLength(100);
        });

        // AppSetting
        builder.Entity<AppSetting>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Value).HasMaxLength(2000);
        });

        // Permission
        builder.Entity<Permission>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Category).IsRequired().HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasMany(x => x.RolePermissions)
             .WithOne(x => x.Permission)
             .HasForeignKey(x => x.PermissionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.UserPermissions)
             .WithOne(x => x.Permission)
             .HasForeignKey(x => x.PermissionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // RolePermission
        builder.Entity<RolePermission>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
            e.HasOne(x => x.Role)
             .WithMany()
             .HasForeignKey(x => x.RoleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // UserPermission
        builder.Entity<UserPermission>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.PermissionId }).IsUnique();
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
