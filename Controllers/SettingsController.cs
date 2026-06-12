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

        var model = new SettingsViewModel
        {
            ReportTemplates = templates,
            CompanyName = await GetSettingAsync("CompanyName", "سیستم مدیریت فاکتور"),
            CompanyPhone = await GetSettingAsync("CompanyPhone", ""),
            CompanyAddress = await GetSettingAsync("CompanyAddress", ""),
            CompanyEconomicCode = await GetSettingAsync("CompanyEconomicCode", ""),
            DefaultFactorPrintTemplateId = await GetSettingIntAsync("DefaultFactorPrintTemplateId")
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
        await SetSettingAsync("DefaultFactorPrintTemplateId", model.DefaultFactorPrintTemplateId?.ToString() ?? "");

        TempData["Success"] = "تنظیمات با موفقیت ذخیره شد";
        return RedirectToAction("Index");
    }

    // Public helper: get a setting value
    public static async Task<string> GetSettingAsync(AppDbContext context, string key, string defaultValue = "")
    {
        var setting = await context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value ?? defaultValue;
    }

    // Public helper: get a setting as int
    public static async Task<int?> GetSettingIntAsync(AppDbContext context, string key)
    {
        var setting = await context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting?.Value != null && int.TryParse(setting.Value, out int result))
            return result;
        return null;
    }

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
