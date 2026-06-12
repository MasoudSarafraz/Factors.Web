using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Controllers;

[Authorize(Roles = "Admin")]
public class SettingsController : Controller
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var templates = await _context.ReportTemplates
            .OrderBy(t => t.Name)
            .Select(t => new ReportTemplateOption
            {
                Id = t.Id,
                Name = t.Name,
                TemplateType = (int)t.TemplateType,
                TemplateTypeName = t.TemplateType.ToString()
            })
            .ToListAsync();

        // Load FactorPrintTemplateIds (comma-separated)
        var templateIds = await GetSettingIntListAsync(_context, "FactorPrintTemplateIds");

        // Migration: if new key is empty but old key exists, migrate
        if (!templateIds.Any())
        {
            var oldId = await GetSettingIntAsync(_context, "DefaultFactorPrintTemplateId");
            if (oldId.HasValue)
            {
                templateIds = new List<int> { oldId.Value };
            }
        }

        var model = new SettingsViewModel
        {
            ReportTemplates = templates,
            FactorPrintTemplateIds = templateIds,
            CompanyName = await GetSettingAsync(_context, "CompanyName", "سیستم مدیریت فاکتور"),
            CompanyPhone = await GetSettingAsync(_context, "CompanyPhone", ""),
            CompanyAddress = await GetSettingAsync(_context, "CompanyAddress", ""),
            CompanyEconomicCode = await GetSettingAsync(_context, "CompanyEconomicCode", "")
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsSaveViewModel model)
    {
        await SetSettingAsync("CompanyName", model.CompanyName ?? "سیستم مدیریت فاکتور");
        await SetSettingAsync("CompanyPhone", model.CompanyPhone ?? "");
        await SetSettingAsync("CompanyAddress", model.CompanyAddress ?? "");
        await SetSettingAsync("CompanyEconomicCode", model.CompanyEconomicCode ?? "");

        // Save FactorPrintTemplateIds as comma-separated string
        var ids = model.FactorPrintTemplateIds ?? new List<int>();
        await SetSettingAsync("FactorPrintTemplateIds", string.Join(",", ids));

        // Also update the old key for backward compatibility
        var firstId = ids.FirstOrDefault();
        await SetSettingAsync("DefaultFactorPrintTemplateId", firstId > 0 ? firstId.ToString() : "");

        TempData["Success"] = "تنظیمات با موفقیت ذخیره شد";
        return RedirectToAction("Index");
    }

    // ── Public static helpers (used by other controllers) ──

    public static async Task<string> GetSettingAsync(AppDbContext context, string key, string defaultValue = "")
    {
        var setting = await context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value ?? defaultValue;
    }

    public static async Task<int?> GetSettingIntAsync(AppDbContext context, string key)
    {
        var setting = await context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting?.Value != null && int.TryParse(setting.Value, out int result))
            return result;
        return null;
    }

    /// <summary>
    /// Reads a comma-separated integer list from AppSettings.
    /// Handles both old single-value format and new comma-separated format.
    /// </summary>
    public static async Task<List<int>> GetSettingIntListAsync(AppDbContext context, string key)
    {
        var setting = await context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting?.Value != null)
        {
            return setting.Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => int.TryParse(v.Trim(), out int id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }
        return new List<int>();
    }

    /// <summary>
    /// Loads the configured print templates for factor views.
    /// Returns a list of PrintTemplateOption. If no templates are configured,
    /// returns a single "PDF" option.
    /// </summary>
    public static async Task<List<PrintTemplateOption>> GetPrintTemplatesAsync(AppDbContext context)
    {
        var templateIds = await GetSettingIntListAsync(context, "FactorPrintTemplateIds");

        // Migration: if new key is empty but old key exists
        if (!templateIds.Any())
        {
            var oldId = await GetSettingIntAsync(context, "DefaultFactorPrintTemplateId");
            if (oldId.HasValue)
            {
                templateIds = new List<int> { oldId.Value };
            }
        }

        var result = new List<PrintTemplateOption>();

        if (templateIds.Any())
        {
            var templates = await context.ReportTemplates
                .Where(t => templateIds.Contains(t.Id))
                .OrderBy(t => t.Name)
                .Select(t => new PrintTemplateOption { Id = t.Id, Name = t.Name })
                .ToListAsync();
            result.AddRange(templates);
        }

        // Always add PDF option
        result.Add(new PrintTemplateOption { Id = 0, Name = "PDF پیش‌فرض" });

        return result;
    }

    // ── Private instance helpers ──

    private async Task<string> GetSettingAsync(string key, string defaultValue = "")
    {
        var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value ?? defaultValue;
    }

    private async Task<int?> GetSettingIntAsync(string key)
    {
        var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting?.Value != null && int.TryParse(setting.Value, out int result))
            return result;
        return null;
    }

    private async Task SetSettingAsync(string key, string value)
    {
        var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            _context.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
            _context.AppSettings.Update(setting);
        }
        await _context.SaveChangesAsync();
    }
}
