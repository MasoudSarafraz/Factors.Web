using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Factors.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Controllers;

[Authorize]
public class ReportBuilderController : Controller
{
    private readonly AppDbContext _context;
    private readonly IReportTemplateService _templateService;
    private readonly ILogger<ReportBuilderController> _logger;

    public ReportBuilderController(
        AppDbContext context,
        IReportTemplateService templateService,
        ILogger<ReportBuilderController> logger)
    {
        _context = context;
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// مرکز گزارش‌سازی - صفحه اصلی با فلوی حرفه‌ای
    /// </summary>
    public async Task<IActionResult> Index(ReportTemplateType? type = null)
    {
        var templates = await _templateService.GetAvailableTemplatesAsync(type);
        var persons = await _context.Persons
            .OrderBy(p => p.PersonName)
            .Select(p => new { p.Id, p.PersonName, p.IsIndividual })
            .ToListAsync();

        var categories = await _context.ProductCategories
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        var model = new ReportCenterViewModel
        {
            Templates = templates,
            FilterTemplateType = type
        };

        ViewBag.Persons = persons;
        ViewBag.Categories = categories;
        return View(model);
    }

    /// <summary>
    /// جستجوی فاکتورها (AJAX) - فیلتر پیشرفته
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchFactors(string? search, string? fromDateJalali, string? toDateJalali, int? personId)
    {
        var results = await _templateService.SearchFactorsAsync(search, fromDateJalali, toDateJalali, personId);
        return Json(results);
    }

    /// <summary>
    /// جستجوی محصولات (AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchProducts(string? search, int? categoryId)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Code.Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await query
            .OrderBy(p => p.Name)
            .Take(100)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Code,
                CategoryName = p.Category != null ? p.Category.Name : ""
            })
            .ToListAsync();

        return Json(products);
    }

    /// <summary>
    /// جستجوی اشخاص (AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchPersons(string? search, bool? isIndividual)
    {
        var query = _context.Persons.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.PersonName.Contains(search));
        }

        if (isIndividual.HasValue)
        {
            query = query.Where(p => p.IsIndividual == isIndividual.Value);
        }

        var persons = await query
            .OrderBy(p => p.PersonName)
            .Take(100)
            .Select(p => new { p.Id, p.PersonName, p.IsIndividual })
            .ToListAsync();

        return Json(persons);
    }

    /// <summary>
    /// تولید گزارش فاکتور تکی
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateSingleFactorReport(int templateId, int factorId)
    {
        try
        {
            var docBytes = await _templateService.GenerateReportAsync(templateId, factorId);

            var template = await _context.ReportTemplates.FindAsync(templateId);
            var factor = await _context.Factors.FindAsync(factorId);
            var fileName = $"{template?.Name ?? "Report"}-Factor-{factor?.Id ?? 0}.docx";

            return File(docBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تولید گزارش از مرکز گزارش‌سازی");
            return Json(new { success = false, message = $"خطا در تولید گزارش: {ex.Message}" });
        }
    }

    /// <summary>
    /// تولید گزارش گروهی فاکتورها - هر فاکتور در یک فایل جدا
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateBatchFactorReport(int templateId, string? search, string? fromDateJalali, string? toDateJalali, int? personId)
    {
        try
        {
            DateTime? fromDate = PersianDateService.ParsePersianDate(fromDateJalali);
            DateTime? toDate = PersianDateService.ParsePersianDate(toDateJalali);

            var query = _context.Factors
                .Include(f => f.Person)
                .Include(f => f.FactorItems)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(f => f.Person!.PersonName.Contains(search) || f.Id.ToString() == search);

            if (fromDate.HasValue)
                query = query.Where(f => f.CreateDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(f => f.CreateDate <= toDate.Value.AddDays(1));

            if (personId.HasValue)
                query = query.Where(f => f.PersonId == personId.Value);

            var factorIds = await query
                .OrderByDescending(f => f.CreateDate)
                .Take(500)
                .Select(f => f.Id)
                .ToListAsync();

            if (factorIds.Count == 0)
                return Json(new { success = false, message = "فاکتوری با این فیلترها یافت نشد" });

            // اگر فقط یک فاکتور هست، مستقیم خروجی بده
            if (factorIds.Count == 1)
            {
                return await GenerateSingleFactorReport(templateId, factorIds[0]);
            }

            // تولید گزارش‌های گروهی - هر فاکتور یک فایل
            var zipBytes = await _templateService.GenerateBatchReportAsync(templateId, factorIds);
            var template = await _context.ReportTemplates.FindAsync(templateId);
            var fileName = $"{template?.Name ?? "Report"}-Batch-{factorIds.Count}Factors.zip";

            return File(zipBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تولید گزارش گروهی");
            return Json(new { success = false, message = $"خطا در تولید گزارش: {ex.Message}" });
        }
    }

    /// <summary>
    /// دریافت لیست قالب‌ها بر اساس نوع (AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTemplatesByType(ReportTemplateType type)
    {
        var templates = await _templateService.GetAvailableTemplatesAsync(type);
        return Json(templates);
    }
}
