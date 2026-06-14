using Factors.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Factors.Web.Infrastructure;

/// <summary>
/// هلپر برای بررسی دسترسی‌ها در View ها (Razor)
/// استفاده: @(await Html.HasPermissionAsync("Category.View"))
/// </summary>
public static class PermissionViewHelper
{
    /// <summary>
    /// بررسی اینکه آیا کاربر فعلی دسترسی مشخص‌شده را دارد
    /// </summary>
    public static async Task<bool> HasPermissionAsync(this IHtmlHelper _, string permissionName)
    {
        var httpContext = _.ViewContext.HttpContext;
        var user = httpContext.User;

        if (user.Identity?.IsAuthenticated != true)
            return false;

        // Admin always has access
        if (user.IsInRole("Admin"))
            return true;

        var permissionService = httpContext.RequestServices.GetService<IPermissionService>();
        if (permissionService == null)
            return true; // Fallback

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return false;

        return await permissionService.HasPermissionAsync(userId, permissionName);
    }
}
