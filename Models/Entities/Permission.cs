namespace Factors.Web.Models.Entities;

/// <summary>
/// سطح دسترسی (Permission) - هر دسترسی یک عملیات روی یک بخش از سیستم است
/// </summary>
public class Permission
{
    public int Id { get; set; }
    
    /// <summary>
    /// نام یکتای دسترسی مانند Category.View
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// عنوان نمایشی فارسی مانند «مشاهده دسته‌بندی‌ها»
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// دسته‌بندی دسترسی مانند «دسته‌بندی‌ها»، «محصولات» و غیره
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// توضیحات اختیاری
    /// </summary>
    public string? Description { get; set; }

    // Navigation
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}

/// <summary>
/// ارتباط نقش و دسترسی - تعیین اینکه هر نقش چه دسترسی‌هایی دارد
/// </summary>
public class RolePermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int PermissionId { get; set; }

    // Navigation
    public virtual AppRole Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}

/// <summary>
/// ارتباط کاربر و دسترسی - برای اعطای یا سلب دسترسی فردی
/// IsGranted=true یعنی دسترسی اضافه شده، IsGranted=false یعنی دسترسی سلب شده
/// </summary>
public class UserPermission
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PermissionId { get; set; }
    
    /// <summary>
    /// اگر true باشد دسترسی به کاربر اعطا شده، اگر false باشد دسترسی از کاربر سلب شده
    /// </summary>
    public bool IsGranted { get; set; } = true;

    // Navigation
    public virtual AppUser User { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
