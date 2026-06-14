using Factors.Web.Models.Entities;

namespace Factors.Web.Services;

/// <summary>
/// سرویس مدیریت دسترسی‌ها (RBAC)
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// بررسی اینکه آیا کاربر دسترسی مشخص‌شده را دارد یا خیر
    /// اول دسترسی‌های فردی کاربر بررسی می‌شود (IsGranted=false یعنی سلب شده)
    /// سپس دسترسی‌های نقش‌های کاربر بررسی می‌شود
    /// </summary>
    Task<bool> HasPermissionAsync(int userId, string permissionName);

    /// <summary>
    /// دریافت تمام دسترسی‌های یک کاربر (ترکیب نقش‌ها + دسترسی‌های فردی)
    /// </summary>
    Task<List<Permission>> GetUserPermissionsAsync(int userId);

    /// <summary>
    /// دریافت نام تمام دسترسی‌های یک کاربر
    /// </summary>
    Task<HashSet<string>> GetUserPermissionNamesAsync(int userId);

    /// <summary>
    /// دریافت تمام دسترسی‌های تعریف‌شده در سیستم به‌همراه دسته‌بندی
    /// </summary>
    Task<List<Permission>> GetAllPermissionsAsync();

    /// <summary>
    /// دریافت دسترسی‌های یک نقش
    /// </summary>
    Task<List<Permission>> GetRolePermissionsAsync(int roleId);

    /// <summary>
    /// تنظیم دسترسی‌های یک نقش (جایگزینی کامل)
    /// </summary>
    Task SetRolePermissionsAsync(int roleId, List<int> permissionIds);

    /// <summary>
    /// دریافت دسترسی‌های فردی یک کاربر
    /// </summary>
    Task<List<UserPermission>> GetUserSpecificPermissionsAsync(int userId);

    /// <summary>
    /// تنظیم دسترسی‌های فردی یک کاربر (جایگزینی کامل)
    /// </summary>
    Task SetUserPermissionsAsync(int userId, List<UserPermissionInput> permissions);

    /// <summary>
    /// دریافت تمام دسترسی‌ها به‌صورت گروه‌بندی‌شده بر اساس دسته‌بندی
    /// </summary>
    Task<Dictionary<string, List<Permission>>> GetPermissionsGroupedByCategoryAsync();
}

/// <summary>
/// مدل ورودی برای تنظیم دسترسی فردی کاربر
/// </summary>
public class UserPermissionInput
{
    public int PermissionId { get; set; }
    public bool IsGranted { get; set; }
}
