using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Factors.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class ReportTemplateController : Controller
{
    private readonly AppDbContext _context;
    private readonly IReportTemplateService _templateService;
    private readonly ILogger<ReportTemplateController> _logger;

    public ReportTemplateController(
        AppDbContext context,
        IReportTemplateService templateService,
        ILogger<ReportTemplateController> logger)
    {
        _context = context;
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// لیست قالب‌های گزارش
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var model = await _templateService.GetTemplatesAsync();
        return View(model);
    }

    /// <summary>
    /// فرم آپلود قالب جدید
    /// </summary>
    public IActionResult Upload()
    {
        return View(new ReportTemplateUploadViewModel());
    }

    /// <summary>
    /// آپلود قالب جدید
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(ReportTemplateUploadViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (model.TemplateFile == null || model.TemplateFile.Length == 0)
        {
            ModelState.AddModelError("TemplateFile", "لطفاً فایل قالب را انتخاب کنید");
            return View(model);
        }

        var ext = Path.GetExtension(model.TemplateFile.FileName).ToLower();
        if (ext != ".docx")
        {
            ModelState.AddModelError("TemplateFile", "فقط فایل‌های Word (.docx) پذیرفته می‌شوند");
            return View(model);
        }

        try
        {
            var template = await _templateService.UploadTemplateAsync(
                model.Name, model.Description, model.TemplateFile, model.TemplateType);

            TempData["Success"] = $"قالب «{template.Name}» با موفقیت آپلود شد. اکنون مارکرها را تنظیم کنید.";
            return RedirectToAction("MapMarkers", new { templateId = template.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در آپلود قالب گزارش");
            ModelState.AddModelError("", $"خطا در آپلود قالب: {ex.Message}");
            return View(model);
        }
    }

    /// <summary>
    /// فرم نقشه‌برداری مارکرها به پراپرتی‌ها
    /// </summary>
    public async Task<IActionResult> MapMarkers(int templateId)
    {
        try
        {
            var model = await _templateService.GetMarkerMappingAsync(templateId);
            return View(model);
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// ذخیره نقشه‌برداری مارکرها
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MapMarkers(SaveMarkerMappingViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "داده‌های وارد شده نامعتبر است";
            return RedirectToAction("MapMarkers", new { templateId = model.TemplateId });
        }

        try
        {
            await _templateService.SaveMarkerMappingsAsync(model);
            TempData["Success"] = "نقشه‌برداری مارکرها با موفقیت ذخیره شد";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ذخیره نقشه‌برداری");
            TempData["Error"] = $"خطا: {ex.Message}";
            return RedirectToAction("MapMarkers", new { templateId = model.TemplateId });
        }
    }

    /// <summary>
    /// فرم ویرایش قالب
    /// </summary>
    public async Task<IActionResult> Edit(int id)
    {
        var template = await _context.ReportTemplates.FindAsync(id);
        if (template == null)
        {
            TempData["Error"] = "قالب یافت نشد";
            return RedirectToAction("Index");
        }

        var model = new ReportTemplateEditViewModel
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            TemplateType = template.TemplateType,
            OriginalFileName = template.OriginalFileName
        };

        return View(model);
    }

    /// <summary>
    /// ویرایش قالب
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ReportTemplateEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Validate file extension if provided
        if (model.TemplateFile != null && model.TemplateFile.Length > 0)
        {
            var ext = Path.GetExtension(model.TemplateFile.FileName).ToLower();
            if (ext != ".docx")
            {
                ModelState.AddModelError("TemplateFile", "فقط فایل‌های Word (.docx) پذیرفته می‌شوند");
                return View(model);
            }
        }

        try
        {
            var template = await _templateService.UpdateTemplateAsync(
                model.Id, model.Name, model.Description, model.TemplateType, model.TemplateFile);

            TempData["Success"] = $"قالب «{template.Name}» با موفقیت ویرایش شد";
            return RedirectToAction("Index");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ویرایش قالب گزارش");
            ModelState.AddModelError("", $"خطا در ویرایش قالب: {ex.Message}");
            return View(model);
        }
    }

    /// <summary>
    /// حذف قالب
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _templateService.DeleteTemplateAsync(id);
            TempData["Success"] = "قالب با موفقیت حذف شد";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در حذف قالب");
            TempData["Error"] = $"خطا در حذف قالب: {ex.Message}";
        }
        return RedirectToAction("Index");
    }

    /// <summary>
    /// دانلود فایل قالب اصلی
    /// </summary>
    public async Task<IActionResult> Download(int id)
    {
        var template = await _context.ReportTemplates.FindAsync(id);
        if (template == null || string.IsNullOrEmpty(template.FilePath) || !System.IO.File.Exists(template.FilePath))
        {
            TempData["Error"] = "فایل قالب یافت نشد";
            return RedirectToAction("Index");
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(template.FilePath);
        return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", template.OriginalFileName);
    }
}
