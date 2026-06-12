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

        // فقط فایل Word
        var ext = Path.GetExtension(model.TemplateFile.FileName).ToLower();
        if (ext != ".docx")
        {
            ModelState.AddModelError("TemplateFile", "فقط فایل‌های Word (.docx) پذیرفته می‌شوند");
            return View(model);
        }

        try
        {
            var template = await _templateService.UploadTemplateAsync(
                model.Name, model.Description, model.TemplateFile);

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
    /// فرم تولید گزارش از قالب
    /// </summary>
    public async Task<IActionResult> Generate(int id)
    {
        var template = await _context.ReportTemplates
            .Include(t => t.Markers)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
        {
            TempData["Error"] = "قالب یافت نشد";
            return RedirectToAction("Index");
        }

        var model = new GenerateReportViewModel
        {
            TemplateId = template.Id,
            TemplateName = template.Name
        };

        // Materialize first, then project (avoid CS0854: optional args in expression tree)
        var factorsData = await _context.Factors
            .Include(f => f.Person)
            .Include(f => f.FactorItems)
            .OrderByDescending(f => f.CreateDate)
            .ToListAsync();

        ViewBag.Factors = factorsData.Select(f => new
        {
            f.Id,
            PersonName = f.Person != null ? f.Person.PersonName : "",
            PersianDate = PersianDateService.ToPersian(f.CreateDate),
            TotalAmount = f.FactorItems.Sum(fi => fi.Price * fi.Qty)
        }).ToList();

        return View(model);
    }

    /// <summary>
    /// تولید گزارش
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(GenerateReportViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Materialize first, then project (avoid CS0854)
            var factorsData = await _context.Factors
                .Include(f => f.Person)
                .Include(f => f.FactorItems)
                .OrderByDescending(f => f.CreateDate)
                .ToListAsync();

            ViewBag.Factors = factorsData.Select(f => new
            {
                f.Id,
                PersonName = f.Person != null ? f.Person.PersonName : "",
                PersianDate = PersianDateService.ToPersian(f.CreateDate),
                TotalAmount = f.FactorItems.Sum(fi => fi.Price * fi.Qty)
            }).ToList();
            return View(model);
        }

        try
        {
            var pdfBytes = await _templateService.GenerateReportAsync(model.TemplateId, model.FactorId);

            var template = await _context.ReportTemplates.FindAsync(model.TemplateId);
            var factor = await _context.Factors.FindAsync(model.FactorId);
            var fileName = $"{template?.Name ?? "Report"}-Factor-{factor?.Id ?? 0}.docx";

            return File(pdfBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تولید گزارش از قالب");
            TempData["Error"] = $"خطا در تولید گزارش: {ex.Message}";
            return RedirectToAction("Generate", new { id = model.TemplateId });
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
