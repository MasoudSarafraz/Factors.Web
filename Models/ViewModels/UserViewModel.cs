using System.ComponentModel.DataAnnotations;

namespace Factors.Web.Models.ViewModels;

public class UserViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Roles { get; set; } = string.Empty;
    public string PersianCreateDate { get; set; } = string.Empty;
    public DateTime? LastLoginDate { get; set; }
}

public class UserCreateViewModel
{
    [Required(ErrorMessage = "نام کامل الزامی است")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "نام کاربری الزامی است")]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "ایمیل الزامی است")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز عبور الزامی است")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "رمز عبور باید حداقل 6 کاراکتر باشد")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "انتخاب نقش الزامی است")]
    public List<string> SelectedRoles { get; set; } = new();
}

public class UserEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام کامل الزامی است")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "ایمیل الزامی است")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    [StringLength(100, MinimumLength = 6, ErrorMessage = "رمز عبور باید حداقل 6 کاراکتر باشد")]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    public List<string> SelectedRoles { get; set; } = new();
}

public class RoleViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PermissionCount { get; set; }
}

public class UserListViewModel
{
    public List<UserViewModel> Users { get; set; } = new();
    public List<RoleViewModel> AvailableRoles { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
}

// ── RBAC View Models ──

/// <summary>
/// مدل صفحه مدیریت دسترسی نقش
/// </summary>
public class RolePermissionsViewModel
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// دسترسی‌های گروه‌بندی‌شده بر اساس دسته‌بندی
    /// </summary>
    public Dictionary<string, List<PermissionCheckItem>> PermissionsByCategory { get; set; } = new();
}

/// <summary>
/// آیتم چک‌باکس دسترسی
/// </summary>
public class PermissionCheckItem
{
    public int PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsChecked { get; set; }
}

/// <summary>
/// مدل صفحه مدیریت دسترسی‌های فردی کاربر
/// </summary>
public class UserPermissionsViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    
    /// <summary>
    /// دسترسی‌های گروه‌بندی‌شده بر اساس دسته‌بندی
    /// </summary>
    public Dictionary<string, List<UserPermissionCheckItem>> PermissionsByCategory { get; set; } = new();
}

/// <summary>
/// آیتم چک‌باکس دسترسی فردی کاربر
/// </summary>
public class UserPermissionCheckItem
{
    public int PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// آیا این دسترسی از طریق نقش‌ها به کاربر داده شده
    /// </summary>
    public bool IsFromRole { get; set; }
    
    /// <summary>
    /// حالت دسترسی فردی: null = بدون تغییر (از نقش ارث برسد), true = اعطا شده, false = سلب شده
    /// </summary>
    public bool? IsGranted { get; set; }
}

/// <summary>
/// مدل ورودی ذخیره دسترسی‌های فردی کاربر
/// </summary>
public class SaveUserPermissionsViewModel
{
    public int UserId { get; set; }
    public List<UserPermissionEntry> Permissions { get; set; } = new();
}

public class UserPermissionEntry
{
    public int PermissionId { get; set; }
    /// <summary>null = ارث از نقش، true = اعطا، false = سلب</summary>
    public bool? IsGranted { get; set; }
}
