using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Services;

/// <summary>
/// پیاده‌سازی سرویس مدیریت دسترسی‌ها (RBAC)
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly AppDbContext _context;

    public PermissionService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(int userId, string permissionName)
    {
        // 1. Check user-specific permission overrides
        var userPermission = await _context.UserPermissions
            .Include(up => up.Permission)
            .Where(up => up.UserId == userId && up.Permission.Name == permissionName)
            .FirstOrDefaultAsync();

        if (userPermission != null)
        {
            // If explicitly denied, return false (even if role grants it)
            if (!userPermission.IsGranted)
                return false;
            // If explicitly granted, return true
            return true;
        }

        // 2. Check role-based permissions
        var userRoleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (!userRoleIds.Any())
            return false;

        var hasRolePermission = await _context.RolePermissions
            .Include(rp => rp.Permission)
            .AnyAsync(rp => userRoleIds.Contains(rp.RoleId) && rp.Permission.Name == permissionName);

        return hasRolePermission;
    }

    /// <inheritdoc />
    public async Task<List<Permission>> GetUserPermissionsAsync(int userId)
    {
        var permissionNames = await GetUserPermissionNamesAsync(userId);
        return await _context.Permissions
            .Where(p => permissionNames.Contains(p.Name))
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<HashSet<string>> GetUserPermissionNamesAsync(int userId)
    {
        var result = new HashSet<string>();

        // 1. Get permissions from roles
        var userRoleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (userRoleIds.Any())
        {
            var rolePermissionNames = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => userRoleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();

            foreach (var name in rolePermissionNames)
                result.Add(name);
        }

        // 2. Apply user-specific overrides
        var userPermissions = await _context.UserPermissions
            .Include(up => up.Permission)
            .Where(up => up.UserId == userId)
            .ToListAsync();

        foreach (var up in userPermissions)
        {
            if (up.IsGranted)
                result.Add(up.Permission.Name);
            else
                result.Remove(up.Permission.Name);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<Permission>> GetAllPermissionsAsync()
    {
        return await _context.Permissions
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Permission>> GetRolePermissionsAsync(int roleId)
    {
        return await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task SetRolePermissionsAsync(int roleId, List<int> permissionIds)
    {
        // Remove existing
        var existing = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();
        _context.RolePermissions.RemoveRange(existing);

        // Add new
        foreach (var permissionId in permissionIds)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
        }

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<UserPermission>> GetUserSpecificPermissionsAsync(int userId)
    {
        return await _context.UserPermissions
            .Include(up => up.Permission)
            .Where(up => up.UserId == userId)
            .OrderBy(up => up.Permission.Category)
            .ThenBy(up => up.Permission.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task SetUserPermissionsAsync(int userId, List<UserPermissionInput> permissions)
    {
        // Remove existing
        var existing = await _context.UserPermissions
            .Where(up => up.UserId == userId)
            .ToListAsync();
        _context.UserPermissions.RemoveRange(existing);

        // Add new
        foreach (var perm in permissions)
        {
            _context.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionId = perm.PermissionId,
                IsGranted = perm.IsGranted
            });
        }

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, List<Permission>>> GetPermissionsGroupedByCategoryAsync()
    {
        var allPermissions = await GetAllPermissionsAsync();
        return allPermissions
            .GroupBy(p => p.Category)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
