using Factors.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Factors.Web.Migrations;

[DbContext(typeof(Data.AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.11")
            .HasAnnotation("Proxies:ChangeTracking", false)
            .HasAnnotation("Proxies:LazyLoading", false)
            .HasAnnotation("SQLite:Autoincrement", true);

        modelBuilder.Entity("Factors.Web.Models.Entities.AppUser", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<int>("AccessFailedCount")
                .HasColumnType("INTEGER");

            b.Property<string>("ConcurrencyStamp")
                .IsConcurrencyToken()
                .HasColumnType("TEXT");

            b.Property<DateTime>("CreateDate")
                .HasColumnType("TEXT");

            b.Property<string>("Email")
                .HasMaxLength(256)
                .HasColumnType("TEXT");

            b.Property<bool>("EmailConfirmed")
                .HasColumnType("INTEGER");

            b.Property<string>("FullName")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<bool>("IsActive")
                .HasColumnType("INTEGER");

            b.Property<DateTime?>("LastLoginDate")
                .HasColumnType("TEXT");

            b.Property<bool>("LockoutEnabled")
                .HasColumnType("INTEGER");

            b.Property<DateTimeOffset?>("LockoutEnd")
                .HasColumnType("TEXT");

            b.Property<string>("NormalizedEmail")
                .HasMaxLength(256)
                .HasColumnType("TEXT");

            b.Property<string>("NormalizedUserName")
                .HasMaxLength(256)
                .HasColumnType("TEXT");

            b.Property<string>("PasswordHash")
                .HasColumnType("TEXT");

            b.Property<string>("PhoneNumber")
                .HasColumnType("TEXT");

            b.Property<bool>("PhoneNumberConfirmed")
                .HasColumnType("INTEGER");

            b.Property<string>("SecurityStamp")
                .HasColumnType("TEXT");

            b.Property<bool>("TwoFactorEnabled")
                .HasColumnType("INTEGER");

            b.Property<string>("UserName")
                .HasMaxLength(256)
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.HasIndex("NormalizedEmail")
                .HasDatabaseName("IX_Users_NormalizedEmail");

            b.HasIndex("NormalizedUserName")
                .IsUnique()
                .HasDatabaseName("IX_Users_NormalizedUserName")
                .HasFilter("[NormalizedUserName] IS NOT NULL");

            b.ToTable("Users", (string)null);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.AppRole", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<string>("ConcurrencyStamp")
                .IsConcurrencyToken()
                .HasColumnType("TEXT");

            b.Property<DateTime>("CreateDate")
                .HasColumnType("TEXT");

            b.Property<string>("Description")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<string>("Name")
                .HasMaxLength(256)
                .HasColumnType("TEXT");

            b.Property<string>("NormalizedName")
                .HasMaxLength(256)
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.HasIndex("NormalizedName")
                .IsUnique()
                .HasDatabaseName("IX_Roles_NormalizedName")
                .HasFilter("[NormalizedName] IS NOT NULL");

            b.ToTable("Roles", (string)null);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.Permission", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<string>("Category")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            b.Property<string>("Description")
                .HasMaxLength(500)
                .HasColumnType("TEXT");

            b.Property<string>("DisplayName")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("TEXT");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.HasIndex("Name")
                .IsUnique();

            b.ToTable("Permissions", (string)null);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.RolePermission", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<int>("PermissionId")
                .HasColumnType("INTEGER");

            b.Property<int>("RoleId")
                .HasColumnType("INTEGER");

            b.HasKey("Id");

            b.HasIndex("PermissionId");

            b.HasIndex("RoleId", "PermissionId")
                .IsUnique();

            b.ToTable("RolePermissions", (string)null);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.UserPermission", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<bool>("IsGranted")
                .HasColumnType("INTEGER");

            b.Property<int>("PermissionId")
                .HasColumnType("INTEGER");

            b.Property<int>("UserId")
                .HasColumnType("INTEGER");

            b.HasKey("Id");

            b.HasIndex("PermissionId");

            b.HasIndex("UserId", "PermissionId")
                .IsUnique();

            b.ToTable("UserPermissions", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<string>("ClaimType")
                .HasColumnType("TEXT");

            b.Property<string>("ClaimValue")
                .HasColumnType("TEXT");

            b.Property<int>("RoleId")
                .HasColumnType("INTEGER");

            b.HasKey("Id");

            b.HasIndex("RoleId")
                .HasDatabaseName("IX_RoleClaims_RoleId");

            b.ToTable("RoleClaims", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<string>("ClaimType")
                .HasColumnType("TEXT");

            b.Property<string>("ClaimValue")
                .HasColumnType("TEXT");

            b.Property<int>("UserId")
                .HasColumnType("INTEGER");

            b.HasKey("Id");

            b.HasIndex("UserId")
                .HasDatabaseName("IX_UserClaims_UserId");

            b.ToTable("UserClaims", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
        {
            b.Property<string>("LoginProvider")
                .HasColumnType("TEXT");

            b.Property<string>("ProviderKey")
                .HasColumnType("TEXT");

            b.Property<string>("ProviderDisplayName")
                .HasColumnType("TEXT");

            b.Property<int>("UserId")
                .HasColumnType("INTEGER");

            b.HasKey("LoginProvider", "ProviderKey");

            b.HasIndex("UserId")
                .HasDatabaseName("IX_UserLogins_UserId");

            b.ToTable("UserLogins", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
        {
            b.Property<int>("UserId")
                .HasColumnType("INTEGER");

            b.Property<int>("RoleId")
                .HasColumnType("INTEGER");

            b.HasKey("UserId", "RoleId");

            b.HasIndex("RoleId")
                .HasDatabaseName("IX_UserRoles_RoleId");

            b.ToTable("UserRoles", (string)null);
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
        {
            b.Property<int>("UserId")
                .HasColumnType("INTEGER");

            b.Property<string>("LoginProvider")
                .HasColumnType("TEXT");

            b.Property<string>("Name")
                .HasColumnType("TEXT");

            b.Property<string>("Value")
                .HasColumnType("TEXT");

            b.HasKey("UserId", "LoginProvider", "Name");

            b.ToTable("UserTokens", (string)null);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.ProductCategory", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<DateTime>("CreateDate")
                .HasColumnType("TEXT");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.HasIndex("Name")
                .IsUnique()
                .HasDatabaseName("IX_ProductCategories_Name");

            b.ToTable("ProductCategories", (string)null);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.Product", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<int>("CategoryId")
                .HasColumnType("INTEGER");

            b.Property<string>("Code")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("TEXT");

            b.Property<DateTime>("CreateDate")
                .HasColumnType("TEXT");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.HasIndex("CategoryId");

            b.HasIndex("Code")
                .IsUnique()
                .HasDatabaseName("IX_Products_Code");

            b.ToTable("Products", (string)null);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.ProductPrice", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<DateTime>("CreateDate")
                .HasColumnType("TEXT");

            b.Property<DateTime>("EndTime")
                .HasColumnType("TEXT");

            b.Property<decimal>("Price")
                .HasColumnType("DECIMAL(18,2)");

            b.Property<int>("ProductId")
                .HasColumnType("INTEGER");

            b.Property<DateTime>("StartTime")
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.HasIndex("ProductId");

            b.ToTable("ProductPrices", (string)null);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.ProductPack", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<DateTime>("CreateDate")
                .HasColumnType("TEXT");

            b.Property<string>("PackCode")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("TEXT");

            b.Property<string>("PackName")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.HasIndex("PackCode")
                .IsUnique()
                .HasDatabaseName("IX_ProductPacks_PackCode");

            b.ToTable("ProductPacks", (string)null);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.ProductPackItems", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<DateTime>("CreateDate")
                .HasColumnType("TEXT");

            b.Property<int>("PackId")
                .HasColumnType("INTEGER");

            b.Property<decimal>("Price")
                .HasColumnType("DECIMAL(18,2)");

            b.Property<int>("ProductId")
                .HasColumnType("INTEGER");

            b.Property<int>("Qty")
                .HasColumnType("INTEGER");

            b.HasKey("Id");

            b.HasIndex("PackId");

            b.HasIndex("ProductId");

            b.ToTable("ProductPackItems", (string)null);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.Person", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<DateTime>("CreateDate")
                .HasColumnType("TEXT");

            b.Property<bool>("IsIndividual")
                .HasColumnType("INTEGER");

            b.Property<string>("PersonName")
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.ToTable("Persons", (string)null);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.Factor", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<int?>("AppUserId")
                .HasColumnType("INTEGER");

            b.Property<DateTime>("CreateDate")
                .HasColumnType("TEXT");

            b.Property<int>("PersonId")
                .HasColumnType("INTEGER");

            b.HasKey("Id");

            b.HasIndex("AppUserId");

            b.HasIndex("PersonId");

            b.ToTable("Factors", (string)null);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.FactorItems", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<int>("FactorId")
                .HasColumnType("INTEGER");

            b.Property<int?>("PackId")
                .HasColumnType("INTEGER");

            b.Property<int?>("ParentId")
                .HasColumnType("INTEGER");

            b.Property<decimal>("Price")
                .HasColumnType("DECIMAL(18,2)");

            b.Property<int?>("SalableId")
                .HasColumnType("INTEGER");

            b.Property<int>("Qty")
                .HasColumnType("INTEGER");

            b.HasKey("Id");

            b.HasIndex("FactorId");

            b.HasIndex("PackId");

            b.HasIndex("SalableId");

            b.ToTable("FactorItems", (string)null);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.ReportTemplate", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<DateTime>("CreateDate")
                .HasColumnType("TEXT");

            b.Property<string>("Description")
                .HasMaxLength(500)
                .HasColumnType("TEXT");

            b.Property<string>("FilePath")
                .IsRequired()
                .HasColumnType("TEXT");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("TEXT");

            b.Property<string>("OriginalFileName")
                .IsRequired()
                .HasMaxLength(300)
                .HasColumnType("TEXT");

            b.Property<int>("TemplateType")
                .ValueGeneratedOnAdd()
                .HasDefaultValue(ReportTemplateType.SingleFactor)
                .HasColumnType("INTEGER");

            b.HasKey("Id");

            b.ToTable("ReportTemplates", (string)null);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.ReportTemplateMarker", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<int>("DataSource")
                .HasColumnType("INTEGER");

            b.Property<int>("DataType")
                .HasColumnType("INTEGER");

            b.Property<string>("MarkerName")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            b.Property<string?>("ParentListMarker")
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            b.Property<string?>("PropertyPath")
                .HasMaxLength(200)
                .HasColumnType("TEXT");

            b.Property<int>("TemplateId")
                .HasColumnType("INTEGER");

            b.HasKey("Id");

            b.HasIndex("TemplateId");

            b.ToTable("ReportTemplateMarkers", (string)null);
        });

        // ---- Relationships ----

        modelBuilder.Entity("Factors.Web.Models.Entities.AppUser", b =>
        {
            b.HasMany("Factors.Web.Models.Entities.Factor", "CreatedFactors")
                .WithOne()
                .HasForeignKey("AppUserId")
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.RolePermission", b =>
        {
            b.HasOne("Factors.Web.Models.Entities.Permission", "Permission")
                .WithMany("RolePermissions")
                .HasForeignKey("PermissionId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Factors.Web.Models.Entities.AppRole", "Role")
                .WithMany()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.UserPermission", b =>
        {
            b.HasOne("Factors.Web.Models.Entities.Permission", "Permission")
                .WithMany("UserPermissions")
                .HasForeignKey("PermissionId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Factors.Web.Models.Entities.AppUser", "User")
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.Product", b =>
        {
            b.HasOne("Factors.Web.Models.Entities.ProductCategory", "Category")
                .WithMany("Products")
                .HasForeignKey("CategoryId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.ProductPrice", b =>
        {
            b.HasOne("Factors.Web.Models.Entities.Product", "Product")
                .WithMany("ProductPrices")
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.ProductPackItems", b =>
        {
            b.HasOne("Factors.Web.Models.Entities.ProductPack", "Pack")
                .WithMany("PackItems")
                .HasForeignKey("PackId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Factors.Web.Models.Entities.Product", "Product")
                .WithMany("PackItems")
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.Factor", b =>
        {
            b.HasOne("Factors.Web.Models.Entities.Person", "Person")
                .WithMany("Factors")
                .HasForeignKey("PersonId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.FactorItems", b =>
        {
            b.HasOne("Factors.Web.Models.Entities.Factor", "Factor")
                .WithMany("FactorItems")
                .HasForeignKey("FactorId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Factors.Web.Models.Entities.Product", "Product")
                .WithMany("FactorItems")
                .HasForeignKey("SalableId")
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne("Factors.Web.Models.Entities.ProductPack", "ProductPack")
                .WithMany()
                .HasForeignKey("PackId")
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.ReportTemplateMarker", b =>
        {
            b.HasOne("Factors.Web.Models.Entities.ReportTemplate", "Template")
                .WithMany("Markers")
                .HasForeignKey("TemplateId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
        {
            b.HasOne("Factors.Web.Models.Entities.AppRole", null)
                .WithMany()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
        {
            b.HasOne("Factors.Web.Models.Entities.AppUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
        {
            b.HasOne("Factors.Web.Models.Entities.AppUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
        {
            b.HasOne("Factors.Web.Models.Entities.AppRole", null)
                .WithMany()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Factors.Web.Models.Entities.AppUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
        {
            b.HasOne("Factors.Web.Models.Entities.AppUser", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Factors.Web.Models.Entities.AppSetting", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            b.Property<string>("Key")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            b.Property<string>("Value")
                .HasMaxLength(2000)
                .HasColumnType("TEXT");

            b.HasKey("Id");

            b.HasIndex("Key")
                .IsUnique();

            b.ToTable("AppSettings", (string)null);
        });
#pragma warning restore 612, 618
    }
}
