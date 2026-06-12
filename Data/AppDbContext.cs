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
    }
}
