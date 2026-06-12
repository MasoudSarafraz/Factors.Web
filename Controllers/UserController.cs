using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly AppDbContext _context;

    public UserController(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, AppDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
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
            .Select(u => new UserViewModel
            {
                Id = u.Id,
                Username = u.UserName ?? "",
                FullName = u.FullName,
                Email = u.Email ?? "",
                IsActive = u.IsActive,
                PersianCreateDate = PersianDateService.ToPersian(u.CreateDate),
                LastLoginDate = u.LastLoginDate
            })
            .ToListAsync();

        // Add roles
        foreach (var user in userList)
        {
            var appUser = await _userManager.FindByIdAsync(user.Id.ToString());
            if (appUser != null)
            {
                var roles = await _userManager.GetRolesAsync(appUser);
                user.Roles = string.Join(", ", roles);
            }
        }

        var roles = await _roleManager.Roles
            .Select(r => new RoleViewModel { Id = r.Id, Name = r.Name, Description = r.Description })
            .ToListAsync();

        var model = new UserListViewModel
        {
            Users = userList,
            AvailableRoles = roles,
            SearchTerm = search ?? ""
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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

    // Role Management
    [HttpPost]
    [ValidateAntiForgeryToken]
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
}
