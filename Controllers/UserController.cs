using Factors.Web.Data;
using Factors.Web.Infrastructure;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Factors.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Controllers;

[Authorize]
[PermissionAuthorize("User.View")]
public class UserController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly AppDbContext _context;
    private readonly IPermissionService _permissionService;

    public UserController(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, AppDbContext context, IPermissionService permissionService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _permissionService = permissionService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var users = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            users = users.Where(u => u.FullName.Contains(search) || u.UserName!.Contains(search) || u.Email!.Contains(search));
        }

        var userList = await users
            .OrderByDescending(u => u.CreateDate)
            .ToListAsync();

        var userViewModels = userList.Select(u => new UserViewModel
        {
            Id = u.Id,
            Username = u.UserName ?? "",
            FullName = u.FullName,
            Email = u.Email ?? "",
            IsActive = u.IsActive,
            PersianCreateDate = PersianDateService.ToPersian(u.CreateDate),
            LastLoginDate = u.LastLoginDate
        }).ToList();

        // Add roles
        foreach (var user in userViewModels)
        {
            var appUser = await _userManager.FindByIdAsync(user.Id.ToString());
            if (appUser != null)
            {
                var userRoles = await _userManager.GetRolesAsync(appUser);
                user.Roles = string.Join(", ", userRoles);
            }
        }

        var availableRoles = await _roleManager.Roles
            .Select(r => new RoleViewModel { Id = r.Id, Name = r.Name, Description = r.Description })
            .ToListAsync();

        // Add permission count per role
        foreach (var role in availableRoles)
        {
            role.PermissionCount = await _context.RolePermissions
                .CountAsync(rp => rp.RoleId == role.Id);
        }

        var model = new UserListViewModel
        {
            Users = userViewModels,
            AvailableRoles = availableRoles,
            SearchTerm = search ?? ""
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermissionAuthorize("User.Create")]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "لطفاً تمام فیلدهای الزامی را پر کنید";
            return RedirectToAction("Index");
        }

        var existingUser = await _userManager.FindByNameAsync(model.Username);
        if (existingUser != null)
        {
            TempData["Error"] = "نام کاربری قبلاً استفاده شده است";
            return RedirectToAction("Index");
        }

        var existingEmail = await _userManager.FindByEmailAsync(model.Email);
        if (existingEmail != null)
        {
            TempData["Error"] = "ایمیل قبلاً استفاده شده است";
            return RedirectToAction("Index");
        }

        var user = new AppUser
        {
            UserName = model.Username,
            Email = model.Email,
            FullName = model.FullName,
            IsActive = true,
            CreateDate = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToAction("Index");
        }

        if (model.SelectedRoles.Any())
        {
            await _userManager.AddToRolesAsync(user, model.SelectedRoles);
        }

        TempData["Success"] = "کاربر با موفقیت ایجاد شد";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return Json(new
        {
            id = user.Id,
            username = user.UserName,
            fullName = user.FullName,
            email = user.Email,
            isActive = user.IsActive,
            roles
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermissionAuthorize("User.Edit")]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "لطفاً تمام فیلدهای الزامی را پر کنید";
            return RedirectToAction("Index");
        }

        var user = await _userManager.FindByIdAsync(model.Id.ToString());
        if (user == null)
            return NotFound();

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.IsActive = model.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            TempData["Error"] = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return RedirectToAction("Index");
        }

        // Update password if provided
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!passwordResult.Succeeded)
            {
                TempData["Error"] = "خطا در تغییر رمز عبور: " + string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                return RedirectToAction("Index");
            }
        }

        // Update roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (model.SelectedRoles.Any())
        {
            await _userManager.AddToRolesAsync(user, model.SelectedRoles);
        }

        TempData["Success"] = "کاربر با موفقیت ویرایش شد";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermissionAuthorize("User.Edit")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = user.IsActive ? "حساب کاربری فعال شد" : "حساب کاربری غیرفعال شد";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermissionAuthorize("User.Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        // Prevent deleting self
        var currentUserId = _userManager.GetUserId(User);
        if (user.Id.ToString() == currentUserId)
        {
            TempData["Error"] = "شما نمی‌توانید حساب کاربری خود را حذف کنید";
            return RedirectToAction("Index");
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = "خطا در حذف کاربر";
            return RedirectToAction("Index");
        }

        TempData["Success"] = "کاربر با موفقیت حذف شد";
        return RedirectToAction("Index");
    }

    // ── Role Management ──

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermissionAuthorize("Role.Manage")]
    public async Task<IActionResult> CreateRole(string roleName, string description)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            TempData["Error"] = "نام نقش الزامی است";
            return RedirectToAction("Index");
        }

        var exists = await _roleManager.RoleExistsAsync(roleName);
        if (exists)
        {
            TempData["Error"] = "این نقش قبلاً وجود دارد";
            return RedirectToAction("Index");
        }

        var result = await _roleManager.CreateAsync(new AppRole
        {
            Name = roleName,
            Description = description ?? "",
            CreateDate = DateTime.UtcNow
        });

        if (!result.Succeeded)
        {
            TempData["Error"] = "خطا در ایجاد نقش";
            return RedirectToAction("Index");
        }

        TempData["Success"] = "نقش با موفقیت ایجاد شد";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermissionAuthorize("Role.Manage")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
        {
            TempData["Error"] = "نقش یافت نشد";
            return RedirectToAction("Index");
        }

        // Prevent deleting built-in Admin role
        if (role.Name == "Admin")
        {
            TempData["Error"] = "نقش ادمین قابل حذف نیست";
            return RedirectToAction("Index");
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
        if (usersInRole.Any())
        {
            TempData["Error"] = $"این نقش به {usersInRole.Count} کاربر اختصاص داده شده و قابل حذف نیست. ابتدا نقش را از کاربران بردارید.";
            return RedirectToAction("Index");
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            TempData["Error"] = "خطا در حذف نقش";
            return RedirectToAction("Index");
        }

        TempData["Success"] = "نقش با موفقیت حذف شد";
        return RedirectToAction("Index");
    }

    // ── Role Permissions Management ──

    [HttpGet]
    [PermissionAuthorize("Role.Manage")]
    public async Task<IActionResult> RolePermissions(int id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            return NotFound();

        var allPermissions = await _permissionService.GetAllPermissionsAsync();
        var rolePermissionIds = await _context.RolePermissions
            .Where(rp => rp.RoleId == id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var permissionsByCategory = allPermissions
            .GroupBy(p => p.Category)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => new PermissionCheckItem
                {
                    PermissionId = p.Id,
                    PermissionName = p.Name,
                    DisplayName = p.DisplayName,
                    Category = p.Category,
                    IsChecked = rolePermissionIds.Contains(p.Id)
                }).ToList()
            );

        var model = new RolePermissionsViewModel
        {
            RoleId = id,
            RoleName = role.Name,
            RoleDescription = role.Description,
            PermissionsByCategory = permissionsByCategory
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermissionAuthorize("Role.Manage")]
    public async Task<IActionResult> RolePermissions(int id, Dictionary<string, List<PermissionCheckItem>> permissionsByCategory)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            return NotFound();

        // Collect all checked permission IDs
        var selectedPermissionIds = permissionsByCategory
            .SelectMany(kvp => kvp.Value)
            .Where(p => p.IsChecked)
            .Select(p => p.PermissionId)
            .ToList();

        await _permissionService.SetRolePermissionsAsync(id, selectedPermissionIds);

        TempData["Success"] = $"دسترسی‌های نقش «{role.Name}» با موفقیت به‌روزرسانی شد";
        return RedirectToAction("Index");
    }

    // ── User Permissions Management ──

    [HttpGet]
    [PermissionAuthorize("User.Edit")]
    public async Task<IActionResult> UserPermissions(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        var allPermissions = await _permissionService.GetAllPermissionsAsync();
        var userRolePermissionNames = await _permissionService.GetUserPermissionNamesAsync(id);

        // Get user-specific overrides
        var userOverrides = await _context.UserPermissions
            .Where(up => up.UserId == id)
            .ToDictionaryAsync(up => up.PermissionId, up => up.IsGranted);

        // Get role-based permissions
        var userRoleIds = await _context.UserRoles
            .Where(ur => ur.UserId == id)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var rolePermissionIds = await _context.RolePermissions
            .Where(rp => userRoleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionId)
            .Distinct()
            .ToListAsync();

        var permissionsByCategory = allPermissions
            .GroupBy(p => p.Category)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p =>
                {
                    bool isFromRole = rolePermissionIds.Contains(p.Id);
                    bool? isGranted = null; // null = inherit from role

                    if (userOverrides.ContainsKey(p.Id))
                    {
                        isGranted = userOverrides[p.Id];
                    }

                    return new UserPermissionCheckItem
                    {
                        PermissionId = p.Id,
                        PermissionName = p.Name,
                        DisplayName = p.DisplayName,
                        Category = p.Category,
                        IsFromRole = isFromRole,
                        IsGranted = isGranted
                    };
                }).ToList()
            );

        var roles = await _userManager.GetRolesAsync(user);

        var model = new UserPermissionsViewModel
        {
            UserId = id,
            Username = user.UserName ?? "",
            FullName = user.FullName,
            Roles = string.Join(", ", roles),
            PermissionsByCategory = permissionsByCategory
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PermissionAuthorize("User.Edit")]
    public async Task<IActionResult> UserPermissions(SaveUserPermissionsViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId.ToString());
        if (user == null)
            return NotFound();

        // Only save permissions that have an override (not null)
        var overrides = model.Permissions
            .Where(p => p.IsGranted.HasValue)
            .Select(p => new UserPermissionInput
            {
                PermissionId = p.PermissionId,
                IsGranted = p.IsGranted!.Value
            })
            .ToList();

        await _permissionService.SetUserPermissionsAsync(model.UserId, overrides);

        TempData["Success"] = $"دسترسی‌های فردی کاربر «{user.FullName}» با موفقیت به‌روزرسانی شد";
        return RedirectToAction("Index");
    }

    // ── API Endpoints ──

    [HttpGet]
    public async Task<IActionResult> GetUserPermissions(int id)
    {
        var permissions = await _permissionService.GetUserPermissionNamesAsync(id);
        return Json(permissions);
    }
}
