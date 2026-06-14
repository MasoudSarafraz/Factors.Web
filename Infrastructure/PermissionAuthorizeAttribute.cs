using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Factors.Web.Infrastructure;

/// <summary>
/// اتریبیوت اعتبارسنجی مبتنی بر Permission - جایگزین [Authorize(Roles = "...")]
/// استفاده: [PermissionAuthorize("Category.View")]
/// چند دسترسی: [PermissionAuthorize("Category.View", "Category.Create")]
/// کاربر باید حداقل یکی از دسترسی‌های مشخص‌شده را داشته باشد
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class PermissionAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    private readonly string[] _permissions;

    public PermissionAuthorizeAttribute(params string[] permissions)
    {
        _permissions = permissions;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        // Admin role always has full access
        if (user.IsInRole("Admin"))
            return;

        var permissionService = context.HttpContext.RequestServices
            .GetService(typeof(Services.IPermissionService)) as Services.IPermissionService;

        if (permissionService == null)
        {
            // Fallback - if service not available, allow through
            return;
        }

        var userId = user.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int uid))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        // Check if user has ANY of the required permissions
        bool hasPermission = _permissions.Any(p => 
            permissionService.HasPermissionAsync(uid, p).GetAwaiter().GetResult());

        if (!hasPermission)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
