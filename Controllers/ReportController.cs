using Factors.Web.Data;
using Factors.Web.Models.Entities;
using Factors.Web.Models.ViewModels;
using Factors.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Factors.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class ReportController : Controller
{
    private readonly AppDbContext _context;
    private readonly IReportService _reportService;

    public ReportController(AppDbContext context, IReportService reportService)
    {
        _context = context;
        _reportService = reportService;
    }

    public IActionResult Index()
    {
        ViewBag.Persons = _context.Persons.OrderBy(p => p.PersonName).ToList();
        ViewBag.Categories = _context.ProductCategories.OrderBy(c => c.Name).ToList();
        ViewBag.Products = _context.Products.OrderBy(p => p.Name).ToList();

        return View(new ReportFilterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Generate(ReportFilterViewModel model)
    {
        // Null safety
        if (string.IsNullOrWhiteSpace(model.ReportType))
        {
            TempData["Error"] = "نوع گزارش مشخص نشده است";
            return RedirectToAction("Index");
        }

        // Parse Jalali dates to Gregorian
        if (!string.IsNullOrWhiteSpace(model.FromDateJalali))
            model.FromDate = PersianDateService.ParsePersianDate(model.FromDateJalali);

        if (!string.IsNullOrWhiteSpace(model.ToDateJalali))
            model.ToDate = PersianDateService.ParsePersianDate(model.ToDateJalali);

        try
        {
            switch (model.ReportType)
            {
                case "factors":
                    return await GenerateFactorReport(model);
                case "products":
                    return await GenerateProductReport(model);
                case "sales":
                    return await GenerateSalesReport(model);
                default:
                    TempData["Error"] = "نوع گزارش نامعتبر است";
                    return RedirectToAction("Index");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Report Error: {ex}");
            TempData["Error"] = $"خطا در تولید گزارش: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    private async Task<IActionResult> GenerateFactorReport(ReportFilterViewModel model)
    {
        var query = _context.Factors
            .Include(f => f.Person)
            .Include(f => f.FactorItems)
            .AsQueryable();

        if (model.FromDate.HasValue)
            query = query.Where(f => f.CreateDate >= model.FromDate.Value);

        if (model.ToDate.HasValue)
            query = query.Where(f => f.CreateDate <= model.ToDate.Value.AddDays(1));

        if (model.PersonId.HasValue)
            query = query.Where(f => f.PersonId == model.PersonId.Value);

        var factors = await query.OrderByDescending(f => f.CreateDate).ToListAsync();

        var factorViewModels = factors.Select(f => new FactorViewModel
        {
            Id = f.Id,
            PersonId = f.PersonId,
            PersonName = f.Person?.PersonName ?? "",
            PersianCreateDate = PersianDateService.ToPersian(f.CreateDate, true),
            TotalAmount = f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0,
            TotalItems = f.FactorItems?.Count ?? 0
        }).ToList();

        var pdfBytes = _reportService.GenerateSalesReportPdf(model, factorViewModels);
        return File(pdfBytes, "application/pdf", "FactorReport.pdf");
    }

    private async Task<IActionResult> GenerateProductReport(ReportFilterViewModel model)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .AsQueryable();

        if (model.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == model.CategoryId.Value);

        if (model.ProductId.HasValue)
            query = query.Where(p => p.Id == model.ProductId.Value);

        var products = await query.OrderBy(p => p.Name).ToListAsync();

        var productViewModels = products.Select(p => new ProductViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? ""
        }).ToList();

        var pdfBytes = _reportService.GenerateProductReportPdf(model, productViewModels);
        return File(pdfBytes, "application/pdf", "ProductsReport.pdf");
    }

    private async Task<IActionResult> GenerateSalesReport(ReportFilterViewModel model)
    {
        var query = _context.Factors
            .Include(f => f.Person)
            .Include(f => f.FactorItems)
            .AsQueryable();

        if (model.FromDate.HasValue)
            query = query.Where(f => f.CreateDate >= model.FromDate.Value);

        if (model.ToDate.HasValue)
            query = query.Where(f => f.CreateDate <= model.ToDate.Value.AddDays(1));

        if (model.PersonId.HasValue)
            query = query.Where(f => f.PersonId == model.PersonId.Value);

        var factors = await query.OrderByDescending(f => f.CreateDate).ToListAsync();

        var factorViewModels = factors.Select(f => new FactorViewModel
        {
            Id = f.Id,
            PersonId = f.PersonId,
            PersonName = f.Person?.PersonName ?? "",
            PersianCreateDate = PersianDateService.ToPersian(f.CreateDate, true),
            TotalAmount = f.FactorItems?.Where(fi => !fi.ParentId.HasValue).Sum(fi => fi.Price * fi.Qty) ?? 0,
            TotalItems = f.FactorItems?.Count ?? 0
        }).ToList();

        var pdfBytes = _reportService.GenerateSalesReportPdf(model, factorViewModels);
        return File(pdfBytes, "application/pdf", "SalesReport.pdf");
    }
}
